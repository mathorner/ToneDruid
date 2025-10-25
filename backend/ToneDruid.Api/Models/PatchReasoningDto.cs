using System.Text.Json.Serialization;

namespace ToneDruid.Api.Models;

public sealed class PatchReasoningDto
{
    [JsonPropertyName("intentSummary")]
    public string? IntentSummary { get; init; }

    [JsonPropertyName("soundDesignNotes")]
    public IReadOnlyList<string>? SoundDesignNotes { get; init; }

    [JsonPropertyName("assumptions")]
    public IReadOnlyList<string>? Assumptions { get; init; }
}
