namespace ToneDruid.Api.Models.VoiceParameters;

public sealed class VoiceParameter
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string GroupId { get; init; }
    public required string GroupLabel { get; init; }
    public required VoiceParameterValueType ValueType { get; init; }
    public VoiceParameterRange? Range { get; init; }
    public IReadOnlyList<string>? AllowedValues { get; init; }
}
