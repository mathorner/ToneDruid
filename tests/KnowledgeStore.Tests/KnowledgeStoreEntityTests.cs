using MinilogueXdValidation.Api.Persistence.Entities;

namespace KnowledgeStore.Tests;

public class KnowledgeStoreEntityTests
{
    [Fact]
    public void KnowledgeDocument_Create_RequiresSourceName()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            KnowledgeDocument.Create(
                sourceName: " ",
                blobPath: "manuals/minilogue-xd.pdf",
                checksum: "abc123",
                pageCount: 100));

        Assert.Equal("sourceName", exception.ParamName);
    }

    [Fact]
    public void KnowledgeDocument_Create_RequiresPositivePageCount()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            KnowledgeDocument.Create(
                sourceName: "Manual",
                blobPath: "manuals/minilogue-xd.pdf",
                checksum: "abc123",
                pageCount: 0));

        Assert.Equal("pageCount", exception.ParamName);
    }

    [Fact]
    public void RetrievalRequest_Create_CapturesUtcTimestamp()
    {
        var before = DateTime.UtcNow;
        var request = RetrievalRequest.Create("describe lfo routing");
        var after = DateTime.UtcNow;

        Assert.InRange(request.CreatedAt, before, after);
        Assert.Equal(DateTimeKind.Utc, request.CreatedAt.Kind);
    }

    [Fact]
    public void FeedbackRating_FromString_RejectsUnknownValues()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => FeedbackRatingExtensions.FromString("neutral"));
        Assert.Equal("value", exception.ParamName);
    }
}
