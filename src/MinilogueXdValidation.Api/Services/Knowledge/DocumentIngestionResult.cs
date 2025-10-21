using MinilogueXdValidation.Api.Persistence.Entities;

namespace MinilogueXdValidation.Api.Services.Knowledge;

public sealed record DocumentIngestionResult(
    KnowledgeDocument Document,
    string BlobLocation,
    string IndexLocation);
