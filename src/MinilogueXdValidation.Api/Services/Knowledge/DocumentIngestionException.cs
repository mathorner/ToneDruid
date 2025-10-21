using MinilogueXdValidation.Api.Persistence.Entities;

namespace MinilogueXdValidation.Api.Services.Knowledge;

public abstract class DocumentIngestionException : Exception
{
    protected DocumentIngestionException(string message)
        : base(message)
    {
    }

    protected DocumentIngestionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class DuplicateDocumentException : DocumentIngestionException
{
    public DuplicateDocumentException(KnowledgeDocument existingDocument)
        : base($"A document with checksum '{existingDocument.Checksum}' has already been ingested.")
    {
        ExistingDocument = existingDocument ?? throw new ArgumentNullException(nameof(existingDocument));
    }

    public KnowledgeDocument ExistingDocument { get; }
}

public sealed class InvalidDocumentException : DocumentIngestionException
{
    public InvalidDocumentException(string message)
        : base(message)
    {
    }
}
