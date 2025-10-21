using MinilogueXdValidation.Api.Persistence.Entities;

namespace KnowledgeStore.Tests;

public static class KnowledgeStoreFixtures
{
    public static KnowledgeDocument CreateDocument(
        string sourceName = "Korg Manual",
        string blobPath = "manuals/minilogue-xd.pdf",
        string checksum = "abc123",
        int pageCount = 120)
    {
        return KnowledgeDocument.Create(sourceName, blobPath, checksum, pageCount);
    }

    public static RetrievalRequest CreateRetrievalRequest(
        string queryText = "explain filter resonance",
        Guid? bestMatchDocumentId = null,
        string? snippet = "Section 5.3: Filter Resonance",
        DateTime? createdAtUtc = null)
    {
        return RetrievalRequest.Create(queryText, bestMatchDocumentId ?? CreateDocument().Id, snippet, createdAtUtc);
    }

    public static Feedback CreateFeedback(
        Guid? retrievalRequestId = null,
        FeedbackRating rating = FeedbackRating.ThumbsUp,
        string? note = "Helpful reference",
        Guid? suggestionId = null,
        DateTime? createdAtUtc = null)
    {
        var requestId = retrievalRequestId ?? CreateRetrievalRequest().Id;
        return Feedback.Create(requestId, rating, note, suggestionId, createdAtUtc);
    }
}
