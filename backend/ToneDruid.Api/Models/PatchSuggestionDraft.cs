namespace ToneDruid.Api.Models;

public sealed class PatchSuggestionDraft
{
    public required string Prompt { get; init; }
    public required string Summary { get; init; }
    public required IReadOnlyList<PatchControlDto> Controls { get; init; }
    public required PatchReasoningDto Reasoning { get; init; }
    public string? RequestId { get; init; }
    public string? ClientRequestId { get; init; }
    public string? GeneratedAtUtc { get; init; }
    public string? Model { get; init; }
}
