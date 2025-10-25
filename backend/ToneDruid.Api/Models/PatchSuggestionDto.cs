namespace ToneDruid.Api.Models;

public sealed class PatchSuggestionDto
{
    public required string Prompt { get; init; }
    public required string Summary { get; init; }
    public required IReadOnlyList<PatchControlDto> Controls { get; init; }
    public required PatchReasoningDto Reasoning { get; init; }
    public required string RequestId { get; init; }
    public required string ClientRequestId { get; init; }
    public required string GeneratedAtUtc { get; init; }
    public required string Model { get; init; }
}
