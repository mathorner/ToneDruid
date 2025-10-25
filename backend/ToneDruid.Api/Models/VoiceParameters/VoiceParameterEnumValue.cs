using System.Text.Json.Serialization;

namespace ToneDruid.Api.Models.VoiceParameters;

public sealed class VoiceParameterEnumValue
{
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("label")]
    public string? Label { get; init; }
}
