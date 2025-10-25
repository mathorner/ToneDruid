using System.Text.Json.Serialization;

namespace ToneDruid.Api.Models.VoiceParameters;

public sealed class VoiceParameterManualReference
{
    [JsonPropertyName("page")]
    public int? Page { get; init; }

    [JsonPropertyName("section")]
    public string? Section { get; init; }
}
