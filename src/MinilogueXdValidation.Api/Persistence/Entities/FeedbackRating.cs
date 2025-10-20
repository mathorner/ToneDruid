namespace MinilogueXdValidation.Api.Persistence.Entities;

public enum FeedbackRating
{
    ThumbsDown = -1,
    ThumbsUp = 1
}

public static class FeedbackRatingExtensions
{
    public static FeedbackRating FromString(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "thumbs_up" => FeedbackRating.ThumbsUp,
            "thumbs_down" => FeedbackRating.ThumbsDown,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported feedback rating.")
        };

    public static string ToPersistedValue(this FeedbackRating rating)
    {
        return rating switch
        {
            FeedbackRating.ThumbsUp => "thumbs_up",
            FeedbackRating.ThumbsDown => "thumbs_down",
            _ => throw new ArgumentOutOfRangeException(nameof(rating), rating, "Unsupported feedback rating.")
        };
    }
}
