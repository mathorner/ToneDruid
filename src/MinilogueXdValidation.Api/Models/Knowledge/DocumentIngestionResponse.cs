namespace MinilogueXdValidation.Api.Models.Knowledge;

public sealed record DocumentIngestionResponse(
    Guid DocumentId,
    string SourceName,
    string BlobLocation,
    string IndexLocation,
    string Checksum,
    int PageCount,
    DateTime CreatedAtUtc);
