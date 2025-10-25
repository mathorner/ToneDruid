using System.Text.Json;
using ToneDruid.Api.Models.VoiceParameters;

namespace ToneDruid.Api.Tests;

public sealed class VoiceParameterEnumValueConverterTests
{
    [Fact]
    public void Deserialize_AcceptsStringValue()
    {
        const string json = @"""Triangle""";

        var result = JsonSerializer.Deserialize<VoiceParameterEnumValue>(json);

        Assert.NotNull(result);
        Assert.Equal("Triangle", result!.Value);
        Assert.Null(result.Label);
    }

    [Fact]
    public void Deserialize_AcceptsObjectValue()
    {
        const string json = @"{""value"":""Saw"",""label"":""Sawtooth""}";

        var result = JsonSerializer.Deserialize<VoiceParameterEnumValue>(json);

        Assert.NotNull(result);
        Assert.Equal("Saw", result!.Value);
        Assert.Equal("Sawtooth", result.Label);
    }

    [Fact]
    public void Deserialize_ConvertsBlankLabelToNull()
    {
        const string json = @"{""value"":""Square"",""label"":""   ""}";

        var result = JsonSerializer.Deserialize<VoiceParameterEnumValue>(json);

        Assert.NotNull(result);
        Assert.Equal("Square", result!.Value);
        Assert.Null(result.Label);
    }

    [Fact]
    public void Serialize_OmitsNullLabel()
    {
        var value = new VoiceParameterEnumValue
        {
            Value = "Sine",
            Label = null
        };

        var json = JsonSerializer.Serialize(value);

        Assert.Equal(@"{""value"":""Sine""}", json);
    }
}
