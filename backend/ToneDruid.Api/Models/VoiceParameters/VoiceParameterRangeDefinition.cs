using System.Text.Json.Serialization;

namespace ToneDruid.Api.Models.VoiceParameters;

public sealed class VoiceParameterRangeDefinition
{
    [JsonPropertyName("min")]
    public double? Min { get; init; }

    [JsonPropertyName("max")]
    public double? Max { get; init; }

    [JsonPropertyName("step")]
    public double? Step { get; init; }

    [JsonPropertyName("unit")]
    public string? Unit { get; init; }
}
