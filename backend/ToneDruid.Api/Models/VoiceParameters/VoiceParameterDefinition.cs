using System.Text.Json.Serialization;

namespace ToneDruid.Api.Models.VoiceParameters;

public sealed class VoiceParameterDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("range")]
    public VoiceParameterRangeDefinition? Range { get; init; }

    [JsonPropertyName("values")]
    public IReadOnlyList<VoiceParameterEnumValue>? Values { get; init; }

    [JsonPropertyName("manual_reference")]
    public VoiceParameterManualReference? ManualReference { get; init; }
}
