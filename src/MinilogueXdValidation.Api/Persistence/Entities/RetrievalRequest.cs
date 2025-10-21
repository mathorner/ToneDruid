namespace MinilogueXdValidation.Api.Persistence.Entities;

public sealed class RetrievalRequest
{
    public Guid Id { get; private set; }
    public string QueryText { get; private set; }
    public Guid? BestMatchDocumentId { get; private set; }
    public string? BestMatchSnippet { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private RetrievalRequest()
    {
        QueryText = string.Empty;
    }

    private RetrievalRequest(Guid id, string queryText, Guid? bestMatchDocumentId, string? bestMatchSnippet, DateTime createdAt)
    {
        Id = id;
        QueryText = queryText;
        BestMatchDocumentId = bestMatchDocumentId;
        BestMatchSnippet = bestMatchSnippet;
        CreatedAt = createdAt;
    }

    public static RetrievalRequest Create(
        string queryText,
        Guid? bestMatchDocumentId = null,
        string? bestMatchSnippet = null,
        DateTime? createdAtUtc = null,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            throw new ArgumentException("Query text is required.", nameof(queryText));
        }

        var utcTimestamp = EnsureUtc(createdAtUtc ?? DateTime.UtcNow);
        return new RetrievalRequest(
            id ?? Guid.NewGuid(),
            queryText.Trim(),
            bestMatchDocumentId,
            string.IsNullOrWhiteSpace(bestMatchSnippet) ? null : bestMatchSnippet.Trim(),
            utcTimestamp);
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
