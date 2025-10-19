using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MinilogueXdValidation.Api.Models;
using MinilogueXdValidation.Api.Schema;

namespace MinilogueXdValidation.Api.Services;

public sealed class MinilogueXdPatchValidator
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly SchemaProvider _schemaProvider;
    private readonly ILogger<MinilogueXdPatchValidator> _logger;

    public MinilogueXdPatchValidator(SchemaProvider schemaProvider, ILogger<MinilogueXdPatchValidator> logger)
    {
        _schemaProvider = schemaProvider;
        _logger = logger;
    }

    public PatchValidationResult Validate(JsonElement patch)
    {
        var errors = new List<ValidationError>();

        if (patch.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new ValidationError("patch", patch.ToString(), "Patch payload must be a JSON object."));
            return PatchValidationResult.Failure(errors);
        }

        var snapshot = _schemaProvider.Snapshot;
        var knownGroups = new HashSet<string>(snapshot.Groups.Keys, Comparer);

        foreach (var section in patch.EnumerateObject())
        {
            if (!knownGroups.Contains(section.Name))
            {
                errors.Add(new ValidationError(section.Name, section.Value.ToString(), $"Unknown section '{section.Name}'."));
                continue;
            }

            if (section.Value.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new ValidationError(section.Name, section.Value.ToString(), $"Section '{section.Name}' must be a JSON object of parameters."));
            }
        }

        // Walk each schema group to ensure required sections exist and parameter values align with the canonical definition.
        foreach (var group in snapshot.Groups.Values)
        {
            if (!patch.TryGetProperty(group.Id, out var groupElement))
            {
                errors.Add(new ValidationError(group.Id, null, $"Required section '{group.Id}' is missing."));
                continue;
            }

            if (groupElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new ValidationError(group.Id, groupElement.ToString(), $"Section '{group.Id}' must be a JSON object of parameters."));
                continue;
            }

            foreach (var parameter in groupElement.EnumerateObject())
            {
                var parameterId = parameter.Name;
                if (!_schemaProvider.TryGetParameter(parameterId, out var definition))
                {
                    errors.Add(new ValidationError(parameterId, parameter.Value.ToString(), $"Unknown parameter '{parameterId}' in section '{group.Id}'."));
                    continue;
                }

                var resolved = _schemaProvider.Resolve(definition);
                ValidateValue(parameterId, parameter.Value, resolved, errors);
            }
        }

        if (errors.Count > 0)
        {
            _logger.LogInformation("Patch validation failed with {ErrorCount} errors.", errors.Count);
            return PatchValidationResult.Failure(errors);
        }

        return PatchValidationResult.Success();
    }

    private static void ValidateValue(string parameterId, JsonElement value, ParameterDefinition definition, ICollection<ValidationError> errors)
    {
        switch (definition.Type)
        {
            case ParameterDataType.Float:
                ValidateNumeric(parameterId, value, definition, errors, requireInteger: false);
                break;

            case ParameterDataType.Integer:
                ValidateNumeric(parameterId, value, definition, errors, requireInteger: true);
                break;

            case ParameterDataType.Boolean:
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    errors.Add(new ValidationError(parameterId, value.ToString(), $"Parameter '{parameterId}' must be a boolean."));
                }
                break;

            case ParameterDataType.Enum:
                ValidateEnum(parameterId, value, definition, errors);
                break;

            case ParameterDataType.List:
                ValidateList(parameterId, value, definition, errors);
                break;

            default:
                errors.Add(new ValidationError(parameterId, value.ToString(), $"Parameter '{parameterId}' has an unsupported schema type '{definition.Type}'."));
                break;
        }
    }

    private static void ValidateNumeric(
        string parameterId,
        JsonElement value,
        ParameterDefinition definition,
        ICollection<ValidationError> errors,
        bool requireInteger)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            var expected = requireInteger ? "integer" : "number";
            errors.Add(new ValidationError(parameterId, value.ToString(), $"Parameter '{parameterId}' must be a {expected}."));
            return;
        }

        var numeric = value.GetDouble();

        if (requireInteger && Math.Abs(numeric - Math.Round(numeric)) > double.Epsilon)
        {
            errors.Add(new ValidationError(parameterId, numeric.ToString(CultureInfo.InvariantCulture), $"Parameter '{parameterId}' must be an integer value."));
            return;
        }

        if (definition.Range is { } range && (numeric < range.Min || numeric > range.Max))
        {
            var message = $"Value for '{parameterId}' must be between {range.Min.ToString(CultureInfo.InvariantCulture)} and {range.Max.ToString(CultureInfo.InvariantCulture)}.";
            errors.Add(new ValidationError(parameterId, numeric.ToString(CultureInfo.InvariantCulture), message));
        }
    }

    private static void ValidateEnum(string parameterId, JsonElement value, ParameterDefinition definition, ICollection<ValidationError> errors)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add(new ValidationError(parameterId, value.ToString(), $"Parameter '{parameterId}' must be a string matching one of the allowed values."));
            return;
        }

        var actual = value.GetString();

        if (string.IsNullOrWhiteSpace(actual))
        {
            errors.Add(new ValidationError(parameterId, actual, $"Parameter '{parameterId}' must specify one of the allowed values."));
            return;
        }

        if (definition.AllowedValues.Count > 0 && !definition.AllowedValues.Contains(actual, Comparer))
        {
            var allowed = string.Join(", ", definition.AllowedValues);
            errors.Add(new ValidationError(parameterId, actual, $"Parameter '{parameterId}' must be one of: {allowed}."));
        }
    }

    private static void ValidateList(string parameterId, JsonElement value, ParameterDefinition definition, ICollection<ValidationError> errors)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new ValidationError(parameterId, value.ToString(), $"Parameter '{parameterId}' must be an array of allowed values."));
            return;
        }

        var allowed = definition.AllowedValues;
        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.String)
            {
                errors.Add(new ValidationError(parameterId, entry.ToString(), $"Each entry in '{parameterId}' must be a string value."));
                continue;
            }

            var actual = entry.GetString();
            if (allowed.Count > 0 && !allowed.Contains(actual, Comparer))
            {
                var allowedValues = string.Join(", ", allowed);
                errors.Add(new ValidationError(parameterId, actual, $"Value '{actual}' in '{parameterId}' must be one of: {allowedValues}."));
            }
        }
    }
}
