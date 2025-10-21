namespace MinilogueXdValidation.Api.Services.Knowledge;

public interface IDocumentIngestionStorage
{
    Task<string> UploadSourceDocumentAsync(string sourceName, string checksum, string fileName, Stream content, CancellationToken cancellationToken);
    Task<string> UploadIndexAsync(string sourceName, string checksum, IReadOnlyList<DocumentPage> pages, CancellationToken cancellationToken);
}
