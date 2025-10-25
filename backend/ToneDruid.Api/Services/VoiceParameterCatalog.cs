using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using ToneDruid.Api.Models.VoiceParameters;

namespace ToneDruid.Api.Services;

public sealed class VoiceParameterCatalog : IVoiceParameterCatalog
{
    private readonly IReadOnlyList<VoiceParameter> _parameters;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<VoiceParameter>> _parametersByGroup;

    public VoiceParameterCatalog(IHostEnvironment environment)
    {
        var resourcePath = Path.Combine(environment.ContentRootPath, "Resources", "voice-parameters.json");
        if (!File.Exists(resourcePath))
        {
            throw new FileNotFoundException($"Voice parameter catalog not found at {resourcePath}");
        }

        using var stream = File.OpenRead(resourcePath);
        var document = JsonSerializer.Deserialize<VoiceParameterCatalogFile>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            }) ?? throw new InvalidOperationException("Unable to deserialize voice parameter catalog.");

        var parameters = new List<VoiceParameter>();
        foreach (var group in document.ParameterGroups)
        {
            foreach (var parameter in group.Parameters)
            {
                var valueType = ResolveValueType(parameter.Type);
                var allowedValues = parameter.Values?.Select(v => v.Value).ToArray();
                VoiceParameterRange? range = null;
                if (parameter.Range?.Min is double min && parameter.Range?.Max is double max)
                {
                    range = new VoiceParameterRange
                    {
                        Min = min,
                        Max = max,
                        Unit = parameter.Range.Unit
                    };
                }

                parameters.Add(new VoiceParameter
                {
                    Id = parameter.Id,
                    Label = parameter.Name,
                    GroupId = group.Id,
                    GroupLabel = group.Label,
                    ValueType = valueType,
                    AllowedValues = allowedValues,
                    Range = range
                });
            }
        }

        _parameters = parameters;
        _parametersByGroup = parameters
            .GroupBy(p => p.GroupId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<VoiceParameter>)g
                    .OrderBy(p => p.Label, StringComparer.OrdinalIgnoreCase)
                    .ToList());
    }

    public IReadOnlyList<VoiceParameter> Parameters => _parameters;

    public VoiceParameter? GetControlById(string id)
    {
        return _parameters.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyDictionary<string, IReadOnlyList<VoiceParameter>> ListByGroup() => _parametersByGroup;

    public IReadOnlyList<VoiceParameter> GetPromptSubset(int limitPerGroup)
    {
        var subset = new List<VoiceParameter>();
        foreach (var group in _parametersByGroup.Values)
        {
            subset.AddRange(group.Take(limitPerGroup));
        }

        return subset;
    }

    public string BuildPromptCatalog(int limitPerGroup)
    {
        var grouped = new ConcurrentDictionary<string, List<VoiceParameter>>();
        foreach (var parameter in GetPromptSubset(limitPerGroup))
        {
            grouped.GetOrAdd(parameter.GroupLabel, _ => new List<VoiceParameter>()).Add(parameter);
        }

        var builder = new StringBuilder();
        foreach (var (groupLabel, controls) in grouped.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"[{groupLabel}]");
            foreach (var control in controls.OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase))
            {
                var entryBuilder = new StringBuilder();
                entryBuilder.Append($"- id: {control.Id}; label: {control.Label}; valueType: {FormatValueType(control.ValueType)}");
                if (control.Range is not null)
                {
                    entryBuilder.Append($"; range: {control.Range.Min}..{control.Range.Max}");
                    if (!string.IsNullOrWhiteSpace(control.Range.Unit))
                    {
                        entryBuilder.Append($" {control.Range.Unit}");
                    }
                }

                if (control.AllowedValues is not null && control.AllowedValues.Count > 0)
                {
                    entryBuilder.Append($"; allowed: {string.Join(", ", control.AllowedValues)}");
                }

                var entry = entryBuilder.ToString();
                if (entry.Length <= 1024)
                {
                    builder.AppendLine(entry);
                }
            }

            builder.AppendLine();
        }

        return builder.ToString().Trim();
    }

    private static VoiceParameterValueType ResolveValueType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "float" or "integer" => VoiceParameterValueType.Continuous,
            "enum" => VoiceParameterValueType.Enumeration,
            "boolean" => VoiceParameterValueType.Boolean,
            _ => VoiceParameterValueType.Continuous
        };
    }

    private static string FormatValueType(VoiceParameterValueType type)
    {
        return type switch
        {
            VoiceParameterValueType.Boolean => "boolean",
            VoiceParameterValueType.Enumeration => "enumeration",
            _ => "continuous"
        };
    }
}
