using System.Text.Json;

namespace ToneDruid.Api.Models;

public sealed class PatchControlDto
{
    public required string Id { get; init; }
    public string? Label { get; init; }
    public string? Group { get; init; }
    public required JsonElement Value { get; init; }
    public required string ValueType { get; init; }
    public PatchControlRangeDto? Range { get; init; }
    public IReadOnlyList<string>? AllowedValues { get; init; }
    public string? Explanation { get; init; }
    public string? Confidence { get; init; }
}
