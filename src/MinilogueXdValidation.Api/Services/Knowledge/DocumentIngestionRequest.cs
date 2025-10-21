namespace MinilogueXdValidation.Api.Services.Knowledge;

public sealed class DocumentIngestionRequest
{
    public DocumentIngestionRequest(string sourceName, string fileName, ReadOnlyMemory<byte> content)
    {
        SourceName = string.IsNullOrWhiteSpace(sourceName)
            ? throw new ArgumentException("Source name is required.", nameof(sourceName))
            : sourceName.Trim();

        FileName = string.IsNullOrWhiteSpace(fileName)
            ? throw new ArgumentException("File name is required.", nameof(fileName))
            : fileName.Trim();

        Content = content;
    }

    public string SourceName { get; }
    public string FileName { get; }
    public ReadOnlyMemory<byte> Content { get; }
}
