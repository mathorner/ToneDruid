using System.Text.Json.Serialization;

namespace ToneDruid.Api.Models.VoiceParameters;

public sealed class VoiceParameterGroup
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("parameters")]
    public required IReadOnlyList<VoiceParameterDefinition> Parameters { get; init; }
}
