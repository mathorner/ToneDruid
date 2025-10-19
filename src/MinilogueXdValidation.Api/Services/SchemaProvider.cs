using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinilogueXdValidation.Api.Schema;

namespace MinilogueXdValidation.Api.Services;

public sealed class SchemaOptions
{
    public string? SchemaPath { get; set; }
}

public sealed class SchemaProvider
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly ILogger<SchemaProvider> _logger;
    private readonly string _schemaPath;
    private readonly Lazy<SchemaSnapshot> _snapshot;

    public SchemaProvider(IOptions<SchemaOptions> options, ILogger<SchemaProvider> logger)
    {
        _logger = logger;
        _schemaPath = options.Value.SchemaPath ?? throw new ArgumentException("Schema path must be provided.", nameof(options));

        _snapshot = new Lazy<SchemaSnapshot>(LoadSchema, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public SchemaSnapshot Snapshot => _snapshot.Value;

    public bool TryGetParameter(string parameterId, out ParameterDefinition definition)
    {
        return Snapshot.ParameterMap.TryGetValue(parameterId, out definition!);
    }

    public SchemaParameterGroup? TryGetGroup(string groupId)
    {
        Snapshot.Groups.TryGetValue(groupId, out var group);
        return group;
    }

    public ParameterDefinition Resolve(ParameterDefinition definition)
    {
        if (definition.Type != ParameterDataType.Alias || string.IsNullOrWhiteSpace(definition.AliasOf))
        {
            return definition;
        }

        var visited = new HashSet<string>(Comparer) { definition.Id };
        var current = definition;

        // Follow alias links until we hit a concrete parameter, guarding against cycles and missing targets.
        while (current.Type == ParameterDataType.Alias && !string.IsNullOrWhiteSpace(current.AliasOf))
        {
            if (!visited.Add(current.AliasOf))
            {
                throw new InvalidOperationException($"Alias cycle detected while resolving parameter '{definition.Id}'.");
            }

            if (!Snapshot.ParameterMap.TryGetValue(current.AliasOf, out var resolved))
            {
                throw new InvalidOperationException($"Alias '{current.Id}' references unknown parameter '{current.AliasOf}'.");
            }

            current = resolved;
        }

        return current;
    }

    private SchemaSnapshot LoadSchema()
    {
        if (!File.Exists(_schemaPath))
        {
            throw new FileNotFoundException($"Schema file not found at path '{_schemaPath}'.");
        }

        using var stream = File.OpenRead(_schemaPath);
        using var document = JsonDocument.Parse(stream);

        var root = document.RootElement;

        if (!root.TryGetProperty("parameter_groups", out var groupsElement) || groupsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Schema file is missing 'parameter_groups' array.");
        }

        var parameterMap = new Dictionary<string, ParameterDefinition>(Comparer);
        var groups = new Dictionary<string, SchemaParameterGroup>(Comparer);

        foreach (var groupElement in groupsElement.EnumerateArray())
        {
            if (!groupElement.TryGetProperty("id", out var idElement) || idElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException("Each parameter group must declare an 'id'.");
            }

            var groupId = idElement.GetString() ?? throw new InvalidOperationException("Group id cannot be null.");
            if (!groupElement.TryGetProperty("parameters", out var parametersElement) || parametersElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"Group '{groupId}' must include a 'parameters' array.");
            }

            var parameterIds = new List<string>();
            foreach (var parameterElement in parametersElement.EnumerateArray())
            {
                var definition = ParseParameter(parameterElement);
                parameterMap[definition.Id] = definition;
                parameterIds.Add(definition.Id);
            }

            groups[groupId] = new SchemaParameterGroup(groupId, parameterIds);
        }

        _logger.LogInformation("Loaded Minilogue XD schema from {SchemaPath} with {GroupCount} groups and {ParameterCount} parameters.", _schemaPath, groups.Count, parameterMap.Count);

        return new SchemaSnapshot(parameterMap, groups);
    }

    private static ParameterDefinition ParseParameter(JsonElement parameterElement)
    {
        var id = parameterElement.GetProperty("id").GetString() ?? throw new InvalidOperationException("Parameter is missing an 'id'.");
        var typeString = parameterElement.GetProperty("type").GetString() ?? throw new InvalidOperationException($"Parameter '{id}' is missing a 'type'.");
        var type = typeString.ToLowerInvariant() switch
        {
            "float" => ParameterDataType.Float,
            "integer" => ParameterDataType.Integer,
            "enum" => ParameterDataType.Enum,
            "boolean" => ParameterDataType.Boolean,
            "list" => ParameterDataType.List,
            "alias" => ParameterDataType.Alias,
            _ => throw new InvalidOperationException($"Unsupported parameter type '{typeString}' for '{id}'.")
        };

        ParameterRange? range = null;
        if (parameterElement.TryGetProperty("range", out var rangeElement) && rangeElement.ValueKind == JsonValueKind.Object)
        {
            var min = rangeElement.GetProperty("min").GetDouble();
            var max = rangeElement.GetProperty("max").GetDouble();
            double? step = null;
            if (rangeElement.TryGetProperty("step", out var stepElement) && stepElement.ValueKind == JsonValueKind.Number)
            {
                step = stepElement.GetDouble();
            }

            range = new ParameterRange(min, max, step);
        }

        IReadOnlyList<string> allowedValues = Array.Empty<string>();
        if (parameterElement.TryGetProperty("values", out var valuesElement) && valuesElement.ValueKind == JsonValueKind.Array)
        {
            var values = new List<string>();
            foreach (var valueNode in valuesElement.EnumerateArray())
            {
                string? value = valueNode.ValueKind switch
                {
                    JsonValueKind.String => valueNode.GetString(),
                    JsonValueKind.Object when valueNode.TryGetProperty("value", out var valueProperty) => valueProperty.GetString(),
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value, Comparer))
                {
                    values.Add(value);
                }
            }

            allowedValues = values;
        }

        string? aliasOf = null;
        if (type == ParameterDataType.Alias && parameterElement.TryGetProperty("alias_of", out var aliasElement) && aliasElement.ValueKind == JsonValueKind.String)
        {
            aliasOf = aliasElement.GetString();
        }

        return new ParameterDefinition(id, type, range, allowedValues, aliasOf);
    }
}
