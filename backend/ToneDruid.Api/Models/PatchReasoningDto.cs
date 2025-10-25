namespace ToneDruid.Api.Models;

public sealed class PatchReasoningDto
{
    public required string IntentSummary { get; init; }
    public required IReadOnlyList<string> SoundDesignNotes { get; init; }
    public required IReadOnlyList<string> Assumptions { get; init; }
}
