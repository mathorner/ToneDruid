using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using MinilogueXdValidation.Api.Persistence.Entities;
using MinilogueXdValidation.Api.Persistence.Repositories;
using MinilogueXdValidation.Api.Services.Knowledge;

namespace KnowledgeStore.Tests;

public sealed class DocumentIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_HappyPath_UploadsAndPersists()
    {
        var parser = new StubParser(pageCount: 2);
        var storage = new StubStorage();
        var repository = new StubRepository();
        var service = CreateService(parser, storage, repository);

        var request = new DocumentIngestionRequest("Minilogue XD Manual", "manual.pdf", CreatePdfBytes());
        var result = await service.IngestAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Document);
        Assert.Equal("Minilogue XD Manual", result.Document.SourceName);
        Assert.Equal(2, result.Document.PageCount);
        Assert.Equal(storage.DocumentLocation, result.BlobLocation);
        Assert.Equal(storage.IndexLocation, result.IndexLocation);
        Assert.NotNull(repository.CreatedDocument);
        Assert.Equal(result.Document.Id, repository.CreatedDocument!.Id);
    }

    [Fact]
    public async Task IngestAsync_WhenChecksumExists_ThrowsDuplicateDocumentException()
    {
        var existing = KnowledgeDocument.Create("Existing Manual", "docs/existing.pdf", "abc123", 10);
        var parser = new StubParser(pageCount: 5);
        var storage = new StubStorage();
        var repository = new StubRepository
        {
            ExistingDocument = existing
        };
        var service = CreateService(parser, storage, repository);
        var request = new DocumentIngestionRequest("Minilogue XD Manual", "manual.pdf", CreatePdfBytes());

        var exception = await Assert.ThrowsAsync<DuplicateDocumentException>(() => service.IngestAsync(request, CancellationToken.None));
        Assert.Equal(existing.Id, exception.ExistingDocument.Id);
    }

    [Fact]
    public async Task IngestAsync_WhenFileIsNotPdf_ThrowsInvalidDocumentException()
    {
        var parser = new StubParser(pageCount: 1);
        var storage = new StubStorage();
        var repository = new StubRepository();
        var service = CreateService(parser, storage, repository);
        var request = new DocumentIngestionRequest("DX Manual", "manual.txt", CreatePdfBytes());

        await Assert.ThrowsAsync<InvalidDocumentException>(() => service.IngestAsync(request, CancellationToken.None));
        Assert.False(storage.Uploaded);
        Assert.Null(repository.CreatedDocument);
    }

    [Fact]
    public async Task IngestAsync_WhenParserReturnsNoPages_ThrowsInvalidDocumentException()
    {
        var parser = new StubParser(pageCount: 0);
        var storage = new StubStorage();
        var repository = new StubRepository();
        var service = CreateService(parser, storage, repository);
        var request = new DocumentIngestionRequest("DX Manual", "manual.pdf", CreatePdfBytes());

        await Assert.ThrowsAsync<InvalidDocumentException>(() => service.IngestAsync(request, CancellationToken.None));
        Assert.False(storage.Uploaded);
        Assert.Null(repository.CreatedDocument);
    }

    private static DocumentIngestionService CreateService(
        IDocumentParser parser,
        IDocumentIngestionStorage storage,
        IKnowledgeDocumentRepository repository)
    {
        return new DocumentIngestionService(parser, storage, repository, NullLogger<DocumentIngestionService>.Instance);
    }

    private static ReadOnlyMemory<byte> CreatePdfBytes()
    {
        return Encoding.UTF8.GetBytes("%PDF-1.4 fake content");
    }

    private sealed class StubParser : IDocumentParser
    {
        private readonly int _pageCount;

        public StubParser(int pageCount)
        {
            _pageCount = pageCount;
        }

        public Task<DocumentParseResult> ParseAsync(Stream documentStream, CancellationToken cancellationToken)
        {
            var pages = Enumerable.Range(1, _pageCount)
                .Select(page => new DocumentPage(page, $"Page {page} content"))
                .ToList();
            return Task.FromResult(new DocumentParseResult(_pageCount, pages));
        }
    }

    private sealed class StubStorage : IDocumentIngestionStorage
    {
        public bool Uploaded { get; private set; }
        public string DocumentLocation { get; private set; } = string.Empty;
        public string IndexLocation { get; private set; } = string.Empty;

        public Task<string> UploadSourceDocumentAsync(string sourceName, string checksum, string fileName, Stream content, CancellationToken cancellationToken)
        {
            Uploaded = true;
            DocumentLocation = $"documents/{checksum}/{fileName}";
            return Task.FromResult(DocumentLocation);
        }

        public Task<string> UploadIndexAsync(string sourceName, string checksum, IReadOnlyList<DocumentPage> pages, CancellationToken cancellationToken)
        {
            IndexLocation = $"indexes/{checksum}/index.json";
            return Task.FromResult(IndexLocation);
        }
    }

    private sealed class StubRepository : IKnowledgeDocumentRepository
    {
        public KnowledgeDocument? ExistingDocument { get; init; }
        public bool SourceExists { get; init; }
        public KnowledgeDocument? CreatedDocument { get; private set; }

        public Task<KnowledgeDocument?> FindByChecksumAsync(string checksum, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingDocument);
        }

        public Task<bool> ExistsForSourceAsync(string sourceName, CancellationToken cancellationToken)
        {
            return Task.FromResult(SourceExists);
        }

        public Task CreateAsync(KnowledgeDocument document, CancellationToken cancellationToken)
        {
            CreatedDocument = document;
            return Task.CompletedTask;
        }
    }
}
