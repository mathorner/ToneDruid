using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinilogueXdValidation.Api.Services.Knowledge;

public sealed class AzureBlobDocumentIngestionStorage : IDocumentIngestionStorage
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly BlobServiceClient _blobServiceClient;
    private readonly DocumentIngestionOptions _options;
    private readonly ILogger<AzureBlobDocumentIngestionStorage> _logger;

    public AzureBlobDocumentIngestionStorage(
        BlobServiceClient blobServiceClient,
        IOptions<DocumentIngestionOptions> options,
        ILogger<AzureBlobDocumentIngestionStorage> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> UploadSourceDocumentAsync(
        string sourceName,
        string checksum,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var container = await GetContainerAsync(_options.SourceContainerName, cancellationToken).ConfigureAwait(false);
        var blobPath = BuildBlobPath(sourceName, checksum, SanitizeFileName(fileName));
        var blobClient = container.GetBlobClient(blobPath);

        if (content.CanSeek)
        {
            content.Seek(0, SeekOrigin.Begin);
        }

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/pdf"
                }
            },
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Uploaded knowledge document blob to {BlobUri}", blobClient.Uri);
        return $"{container.Name}/{blobPath}";
    }

    public async Task<string> UploadIndexAsync(
        string sourceName,
        string checksum,
        IReadOnlyList<DocumentPage> pages,
        CancellationToken cancellationToken)
    {
        var container = await GetContainerAsync(_options.IndexContainerName, cancellationToken).ConfigureAwait(false);
        var blobPath = BuildBlobPath(sourceName, checksum, "index.json");
        var blobClient = container.GetBlobClient(blobPath);

        var payload = new DocumentIndexPayload
        {
            Source = sourceName,
            Checksum = checksum,
            GeneratedAtUtc = DateTime.UtcNow,
            Pages = pages.Select(page => new DocumentIndexPage
            {
                PageNumber = page.PageNumber,
                Text = page.Text
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/json; charset=utf-8"
                }
            },
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Uploaded document index blob to {BlobUri}", blobClient.Uri);
        return $"{container.Name}/{blobPath}";
    }

    private async Task<BlobContainerClient> GetContainerAsync(string containerName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException("Storage container name cannot be null or empty.");
        }

        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return container;
    }

    private static string BuildBlobPath(string sourceName, string checksum, string fileName)
    {
        var slug = Slugify(sourceName);
        return $"{slug}/{checksum}/{fileName}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = string.IsNullOrWhiteSpace(fileName) ? "document.pdf" : fileName.Trim();
        if (!name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            name += ".pdf";
        }

        return name.Replace(" ", "-", StringComparison.OrdinalIgnoreCase);
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "document";
        }

        var lower = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(lower.Length);

        foreach (var c in lower)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
            }
            else if (c is ' ' or '-' or '_' or '.')
            {
                builder.Append('-');
            }
        }

        return builder.Length == 0 ? "document" : builder.ToString().Trim('-');
    }

    private sealed class DocumentIndexPayload
    {
        public required string Source { get; init; }
        public required string Checksum { get; init; }
        public required DateTime GeneratedAtUtc { get; init; }
        public required IReadOnlyList<DocumentIndexPage> Pages { get; init; }
    }

    private sealed class DocumentIndexPage
    {
        public required int PageNumber { get; init; }
        public required string Text { get; init; }
    }
}
