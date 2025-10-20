namespace MinilogueXdValidation.Api.Persistence.Entities;

public sealed class Feedback
{
    private const int MaxNoteLength = 2000;

    public Guid Id { get; private set; }
    public Guid RetrievalRequestId { get; private set; }
    public Guid? SuggestionId { get; private set; }
    public FeedbackRating Rating { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Feedback()
    {
        // For serializers / ORMs
    }

    private Feedback(
        Guid id,
        Guid retrievalRequestId,
        Guid? suggestionId,
        FeedbackRating rating,
        string? note,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        RetrievalRequestId = retrievalRequestId;
        SuggestionId = suggestionId;
        Rating = rating;
        Note = note;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Feedback Create(
        Guid retrievalRequestId,
        FeedbackRating rating,
        string? note = null,
        Guid? suggestionId = null,
        DateTime? timestampUtc = null,
        Guid? id = null)
    {
        if (retrievalRequestId == Guid.Empty)
        {
            throw new ArgumentException("Retrieval request id is required.", nameof(retrievalRequestId));
        }

        var sanitisedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (sanitisedNote is { Length: > MaxNoteLength })
        {
            throw new ArgumentOutOfRangeException(nameof(note), $"Note cannot exceed {MaxNoteLength} characters.");
        }

        var utcNow = EnsureUtc(timestampUtc ?? DateTime.UtcNow);
        return new Feedback(
            id ?? Guid.NewGuid(),
            retrievalRequestId,
            suggestionId == Guid.Empty ? null : suggestionId,
            rating,
            sanitisedNote,
            utcNow,
            utcNow);
    }

    public void Touch(DateTime? timestampUtc = null)
    {
        UpdatedAt = EnsureUtc(timestampUtc ?? DateTime.UtcNow);
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
