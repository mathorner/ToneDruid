using System.Text.Json;

namespace ToneDruid.Api.Models;

public sealed class PatchControlDto
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Group { get; init; }
    public required JsonElement Value { get; init; }
    public required string ValueType { get; init; }
    public PatchControlRangeDto? Range { get; init; }
    public IReadOnlyList<string>? AllowedValues { get; init; }
    public required string Explanation { get; init; }
    public required string Confidence { get; init; }
}
