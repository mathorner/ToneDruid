using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MinilogueXdValidation.Api.Persistence.Entities;
using MinilogueXdValidation.Api.Persistence.Repositories;

namespace MinilogueXdValidation.Api.Services.Knowledge;

public sealed class DocumentIngestionService
{
    private readonly IDocumentParser _documentParser;
    private readonly IDocumentIngestionStorage _storage;
    private readonly IKnowledgeDocumentRepository _repository;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        IDocumentParser documentParser,
        IDocumentIngestionStorage storage,
        IKnowledgeDocumentRepository repository,
        ILogger<DocumentIngestionService> logger)
    {
        _documentParser = documentParser ?? throw new ArgumentNullException(nameof(documentParser));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentIngestionResult> IngestAsync(DocumentIngestionRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        EnsurePdfFile(request);
        var contentBytes = request.Content.ToArray();
        if (contentBytes.Length == 0)
        {
            throw new InvalidDocumentException("Document content cannot be empty.");
        }

        var checksum = ComputeChecksum(contentBytes);
        _logger.LogInformation("Beginning ingestion for {SourceName} (checksum {Checksum}).", request.SourceName, checksum);

        var existingByChecksum = await _repository.FindByChecksumAsync(checksum, cancellationToken).ConfigureAwait(false);
        if (existingByChecksum is not null)
        {
            _logger.LogWarning("Duplicate document submission detected for checksum {Checksum}.", checksum);
            throw new DuplicateDocumentException(existingByChecksum);
        }

        var sourceExists = await _repository.ExistsForSourceAsync(request.SourceName, cancellationToken).ConfigureAwait(false);
        if (sourceExists)
        {
            _logger.LogWarning("Duplicate document submission detected for source {Source}.", request.SourceName);
            throw new InvalidDocumentException($"A document for source '{request.SourceName}' has already been ingested.");
        }

        DocumentParseResult parseResult;
        try
        {
            await using var parseStream = new MemoryStream(contentBytes, writable: false);
            parseResult = await _documentParser.ParseAsync(parseStream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to parse PDF for source {Source}.", request.SourceName);
            throw new InvalidDocumentException("Unable to parse the supplied PDF document.");
        }

        if (parseResult.PageCount <= 0)
        {
            throw new InvalidDocumentException("No pages were extracted from the supplied document.");
        }

        await using var uploadStream = new MemoryStream(contentBytes, writable: false);
        var blobLocation = await _storage
            .UploadSourceDocumentAsync(request.SourceName, checksum, request.FileName, uploadStream, cancellationToken)
            .ConfigureAwait(false);

        var indexLocation = await _storage
            .UploadIndexAsync(request.SourceName, checksum, parseResult.Pages, cancellationToken)
            .ConfigureAwait(false);

        var document = KnowledgeDocument.Create(request.SourceName, blobLocation, checksum, parseResult.PageCount);
        await _repository.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Successfully ingested {SourceName} with {PageCount} pages.",
            document.SourceName,
            document.PageCount);

        return new DocumentIngestionResult(document, blobLocation, indexLocation);
    }

    private static void EnsurePdfFile(DocumentIngestionRequest request)
    {
        if (!request.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDocumentException("Only PDF documents are supported for ingestion.");
        }
    }

    private static string ComputeChecksum(ReadOnlySpan<byte> content)
    {
        Span<byte> hash = stackalloc byte[32];
        if (!SHA256.TryHashData(content, hash, out _))
        {
            using var sha = SHA256.Create();
            var computed = sha.ComputeHash(content.ToArray());
            return Convert.ToHexString(computed).ToLowerInvariant();
        }

        var builder = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }
}
