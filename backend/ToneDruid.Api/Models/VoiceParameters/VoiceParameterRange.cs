namespace ToneDruid.Api.Models.VoiceParameters;

public sealed class VoiceParameterRange
{
    public required double Min { get; init; }
    public required double Max { get; init; }
    public string? Unit { get; init; }
}
