namespace MinilogueXdValidation.Api.Services.Knowledge;

public sealed class DocumentIngestionOptions
{
    public const string ConfigurationSectionName = "DocumentIngestion";

    public string StorageConnectionString { get; set; } = string.Empty;
    public string SourceContainerName { get; set; } = "knowledge-documents";
    public string IndexContainerName { get; set; } = "knowledge-index";
}
