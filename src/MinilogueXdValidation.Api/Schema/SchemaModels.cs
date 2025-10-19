namespace MinilogueXdValidation.Api.Schema;

public enum ParameterDataType
{
    Float,
    Integer,
    Enum,
    Boolean,
    List,
    Alias
}

public sealed record ParameterRange(double Min, double Max, double? Step);

public sealed class ParameterDefinition
{
    public ParameterDefinition(
        string id,
        ParameterDataType type,
        ParameterRange? range,
        IReadOnlyList<string> allowedValues,
        string? aliasOf)
    {
        Id = id;
        Type = type;
        Range = range;
        AllowedValues = allowedValues;
        AliasOf = aliasOf;
    }

    public string Id { get; }

    public ParameterDataType Type { get; }

    public ParameterRange? Range { get; }

    public IReadOnlyList<string> AllowedValues { get; }

    public string? AliasOf { get; }
}

public sealed class SchemaParameterGroup
{
    public SchemaParameterGroup(string id, IReadOnlyList<string> parameterIds)
    {
        Id = id;
        ParameterIds = parameterIds;
    }

    public string Id { get; }

    public IReadOnlyList<string> ParameterIds { get; }
}

public sealed class SchemaSnapshot
{
    public SchemaSnapshot(
        IReadOnlyDictionary<string, ParameterDefinition> parameterMap,
        IReadOnlyDictionary<string, SchemaParameterGroup> groups)
    {
        ParameterMap = parameterMap;
        Groups = groups;
    }

    public IReadOnlyDictionary<string, ParameterDefinition> ParameterMap { get; }

    public IReadOnlyDictionary<string, SchemaParameterGroup> Groups { get; }
}
