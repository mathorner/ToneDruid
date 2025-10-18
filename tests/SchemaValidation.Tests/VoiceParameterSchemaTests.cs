using System.Text.Json;
using Xunit;

namespace SchemaValidation.Tests;

public sealed class SchemaFixture : IDisposable
{
    public JsonDocument Schema { get; }

    public SchemaFixture()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var schemaPath = Path.Combine(projectRoot, "schemas", "minilogue-xd", "voice-parameters.json");
        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Schema file not found at {schemaPath}");
        }

        var json = File.ReadAllText(schemaPath);
        Schema = JsonDocument.Parse(json);
    }

    public void Dispose()
    {
        Schema.Dispose();
    }
}

public class VoiceParameterSchemaTests : IClassFixture<SchemaFixture>
{
    private readonly JsonElement _root;

    public VoiceParameterSchemaTests(SchemaFixture fixture)
    {
        _root = fixture.Schema.RootElement;
    }

    [Fact]
    public void Metadata_ShouldIncludeInstrumentAndSources()
    {
        Assert.True(_root.TryGetProperty("metadata", out var metadata), "Metadata section missing");
        Assert.Equal("Korg Minilogue XD", metadata.GetProperty("instrument").GetString());

        Assert.True(metadata.TryGetProperty("source", out var source), "Metadata source missing");
        Assert.True(source.TryGetProperty("pages_consulted", out var pages), "pages_consulted missing");
        Assert.Equal(JsonValueKind.Array, pages.ValueKind);
        Assert.True(pages.GetArrayLength() > 0, "Expected at least one referenced manual page");
    }

    [Fact]
    public void Schema_ShouldExposeExpectedParameterGroups()
    {
        Assert.True(_root.TryGetProperty("parameter_groups", out var groupsElement), "parameter_groups missing");
        var groupIds = groupsElement.EnumerateArray()
            .Select(group => group.GetProperty("id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var expected = new HashSet<string>(new[]
        {
            "master",
            "oscillators",
            "mixer",
            "filter",
            "envelopes",
            "lfo",
            "effects",
            "program_edit"
        }, StringComparer.OrdinalIgnoreCase);

        Assert.True(expected.IsSubsetOf(groupIds), "Schema missing one or more required parameter groups");

        Assert.True(_root.TryGetProperty("modulation_sources", out var modSources), "modulation_sources missing");
        Assert.True(modSources.GetArrayLength() >= 5, "Expected at least five modulation sources");
    }

    [Fact]
    public void EnumParameters_ShouldDeclareValues()
    {
        var enumParameters = _root.GetProperty("parameter_groups")
            .EnumerateArray()
            .SelectMany(group => group.GetProperty("parameters").EnumerateArray())
            .Where(parameter => parameter.TryGetProperty("type", out var type) && type.GetString() == "enum");

        foreach (var parameter in enumParameters)
        {
            Assert.True(parameter.TryGetProperty("values", out var values), $"Enum parameter {parameter.GetProperty("id").GetString()} missing values");
            Assert.Equal(JsonValueKind.Array, values.ValueKind);
            Assert.True(values.GetArrayLength() > 0, $"Enum parameter {parameter.GetProperty("id").GetString()} has no values defined");
        }
    }
}
