using System.Diagnostics.CodeAnalysis;

namespace MinilogueXdValidation.Api.Persistence.Entities;

public sealed class KnowledgeDocument
{
    public Guid Id { get; private set; }
    public string SourceName { get; private set; }
    public string BlobPath { get; private set; }
    public string Checksum { get; private set; }
    public int PageCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private KnowledgeDocument()
    {
        // Required by serializers / ORMs.
        SourceName = string.Empty;
        BlobPath = string.Empty;
        Checksum = string.Empty;
    }

    private KnowledgeDocument(Guid id, string sourceName, string blobPath, string checksum, int pageCount, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        SourceName = sourceName;
        BlobPath = blobPath;
        Checksum = checksum;
        PageCount = pageCount;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static KnowledgeDocument Create(
        string sourceName,
        string blobPath,
        string checksum,
        int pageCount,
        DateTime? timestampUtc = null,
        Guid? id = null)
    {
        ValidateRequired(sourceName, nameof(sourceName));
        ValidateRequired(blobPath, nameof(blobPath));
        ValidateRequired(checksum, nameof(checksum));
        if (pageCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), pageCount, "Page count must be greater than zero.");
        }

        var utcNow = EnsureUtc(timestampUtc ?? DateTime.UtcNow);
        return new KnowledgeDocument(
            id ?? Guid.NewGuid(),
            sourceName.Trim(),
            blobPath.Trim(),
            checksum.Trim(),
            pageCount,
            utcNow,
            utcNow);
    }

    public static KnowledgeDocument FromPersistence(
        Guid id,
        string sourceName,
        string blobPath,
        string checksum,
        int pageCount,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        if (pageCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), pageCount, "Page count must be greater than zero.");
        }

        return new KnowledgeDocument(
            id,
            sourceName,
            blobPath,
            checksum,
            pageCount,
            EnsureUtc(createdAtUtc),
            EnsureUtc(updatedAtUtc));
    }

    public void Touch(DateTime? timestampUtc = null)
    {
        UpdatedAt = EnsureUtc(timestampUtc ?? DateTime.UtcNow);
    }

    private static void ValidateRequired([NotNull] string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => value
        };
    }
}
