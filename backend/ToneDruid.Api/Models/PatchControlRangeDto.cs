namespace ToneDruid.Api.Models;

public sealed class PatchControlRangeDto
{
    public required double Min { get; init; }
    public required double Max { get; init; }
    public string? Unit { get; init; }
}
