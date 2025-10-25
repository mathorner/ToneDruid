using System.Text.Json.Serialization;

namespace ToneDruid.Api.Models.VoiceParameters;

public sealed class VoiceParameterCatalogFile
{
    [JsonPropertyName("parameter_groups")]
    public required IReadOnlyList<VoiceParameterGroup> ParameterGroups { get; init; }
}
