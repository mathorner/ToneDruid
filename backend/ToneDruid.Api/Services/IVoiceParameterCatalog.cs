using ToneDruid.Api.Models.VoiceParameters;

namespace ToneDruid.Api.Services;

public interface IVoiceParameterCatalog
{
    IReadOnlyList<VoiceParameter> Parameters { get; }

    VoiceParameter? GetControlById(string id);

    IReadOnlyDictionary<string, IReadOnlyList<VoiceParameter>> ListByGroup();

    IReadOnlyList<VoiceParameter> GetPromptSubset(int limitPerGroup);

    string BuildPromptCatalog(int limitPerGroup);
}
