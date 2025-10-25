using System.Text.Json;
using ToneDruid.Api.Agents;
using ToneDruid.Api.Models;

namespace ToneDruid.Api.Tests;

public sealed class PatchReasoningTests
{
    [Fact]
    public void Deserialize_AllowsMissingFields()
    {
        const string json = @"{}";

        var dto = JsonSerializer.Deserialize<PatchReasoningDto>(json);

        Assert.NotNull(dto);
        Assert.Null(dto!.IntentSummary);
        Assert.Null(dto.SoundDesignNotes);
        Assert.Null(dto.Assumptions);
    }

    [Fact]
    public void NormalizeReasoning_FillsDefaults_WhenReasoningIsNull()
    {
        var normalized = PatchGenerationAgent.NormalizeReasoning(null);

        Assert.Equal("Model did not provide an intent summary.", normalized.IntentSummary);
        var notes = Assert.IsAssignableFrom<IReadOnlyList<string>>(normalized.SoundDesignNotes);
        Assert.Single(notes);
        Assert.Equal("Model did not provide detailed sound design notes.", notes[0]);
        var assumptions = Assert.IsAssignableFrom<IReadOnlyList<string>>(normalized.Assumptions);
        Assert.Empty(assumptions);
    }

    [Fact]
    public void NormalizeReasoning_TrimsAndFiltersValues()
    {
        var reasoning = new PatchReasoningDto
        {
            IntentSummary = "Warm evolving pad",
            SoundDesignNotes = new[] { "  use slow attack for fade-in  ", "   " },
            Assumptions = new[] { "  Performing in a large hall  ", "" }
        };

        var normalized = PatchGenerationAgent.NormalizeReasoning(reasoning);

        Assert.Equal("Warm evolving pad", normalized.IntentSummary);
        var notes = Assert.IsAssignableFrom<IReadOnlyList<string>>(normalized.SoundDesignNotes);
        Assert.Single(notes);
        Assert.Equal("use slow attack for fade-in", notes[0]);
        var assumptions = Assert.IsAssignableFrom<IReadOnlyList<string>>(normalized.Assumptions);
        Assert.Single(assumptions);
        Assert.Equal("Performing in a large hall", assumptions[0]);
    }
}
