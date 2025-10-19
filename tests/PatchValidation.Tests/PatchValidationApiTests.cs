using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PatchValidation.Tests;

public class PatchValidationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public PatchValidationApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ValidPatch_ReturnsValidTrue()
    {
        var patch = PatchFixtures.CreateValidPatch();

        var response = await _client.PostAsJsonAsync("/api/v1/minilogue-xd/patches/validate", patch);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ValidationResponse>(SerializerOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Valid);
        Assert.True(payload.Errors == null || payload.Errors.Count == 0);
    }

    [Fact]
    public async Task InvalidEnum_ReturnsSpecificError()
    {
        var patch = PatchFixtures.CreateValidPatch();
        var oscillators = PatchFixtures.GetSection(patch, "oscillators");
        oscillators["vco1.wave"] = "sine";

        var response = await _client.PostAsJsonAsync("/api/v1/minilogue-xd/patches/validate", patch);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ValidationResponse>(SerializerOptions);
        Assert.NotNull(payload);
        Assert.False(payload!.Valid);
        Assert.NotNull(payload.Errors);
        var error = Assert.Single(payload.Errors!, e => e.Field == "vco1.wave");
        Assert.Contains("one of", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("sine", error.Value);
    }

    [Fact]
    public async Task OutOfRangeNumeric_ReturnsGuidance()
    {
        var patch = PatchFixtures.CreateValidPatch();
        var master = PatchFixtures.GetSection(patch, "master");
        master["master.tempo"] = 400;

        var response = await _client.PostAsJsonAsync("/api/v1/minilogue-xd/patches/validate", patch);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ValidationResponse>(SerializerOptions);
        Assert.NotNull(payload);
        Assert.False(payload!.Valid);
        Assert.NotNull(payload.Errors);
        var error = Assert.Single(payload.Errors!, e => e.Field == "master.tempo");
        Assert.Contains("between", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingSection_ReturnsSectionError()
    {
        var patch = PatchFixtures.CreateValidPatch();
        patch.Remove("oscillators");

        var response = await _client.PostAsJsonAsync("/api/v1/minilogue-xd/patches/validate", patch);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ValidationResponse>(SerializerOptions);
        Assert.NotNull(payload);
        Assert.False(payload!.Valid);
        Assert.NotNull(payload.Errors);
        var error = Assert.Single(payload.Errors!, e => e.Field == "oscillators");
        Assert.Contains("missing", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ValidationResponse(bool Valid, List<ValidationErrorResponse>? Errors);

    private sealed record ValidationErrorResponse(string Field, string? Value, string Message);
}

internal static class PatchFixtures
{
    public static Dictionary<string, object> CreateValidPatch()
    {
        return new Dictionary<string, object>
        {
            ["master"] = new Dictionary<string, object>
            {
                ["master.tempo"] = 120.0,
                ["master.portamento"] = 0,
                ["voice_mode.type"] = "poly",
                ["voice_mode.depth"] = 0
            },
            ["oscillators"] = new Dictionary<string, object>
            {
                ["vco1.wave"] = "saw",
                ["vco2.wave"] = "triangle",
                ["multi_engine.mode"] = "noise"
            },
            ["mixer"] = new Dictionary<string, object>
            {
                ["mixer.vco1_level"] = 768,
                ["mixer.vco2_level"] = 640,
                ["mixer.multi_level"] = 512
            },
            ["filter"] = new Dictionary<string, object>
            {
                ["filter.cutoff"] = 512,
                ["filter.resonance"] = 256,
                ["filter.drive"] = "50pct"
            },
            ["envelopes"] = new Dictionary<string, object>
            {
                ["amp_eg.attack"] = 256,
                ["amp_eg.decay"] = 384,
                ["amp_eg.sustain"] = 768,
                ["amp_eg.release"] = 512,
                ["eg.attack"] = 240,
                ["eg.decay"] = 300,
                ["eg.intensity"] = 20,
                ["eg.target"] = "cutoff"
            },
            ["lfo"] = new Dictionary<string, object>
            {
                ["lfo.wave"] = "triangle",
                ["lfo.mode"] = "normal",
                ["lfo.rate"] = 240,
                ["lfo.intensity"] = 0,
                ["lfo.target"] = "cutoff"
            },
            ["effects"] = new Dictionary<string, object>
            {
                ["effects.type"] = "reverb",
                ["effects.variant"] = "on",
                ["effects.time"] = 512,
                ["effects.depth"] = 512,
                ["effects.mix_balance"] = 512
            },
            ["program_edit"] = new Dictionary<string, object>
            {
                ["cv.mode"] = "modulation",
                ["program.multi_octave"] = "8'",
                ["program.portamento_mode"] = "auto",
                ["joystick.assignable_targets"] = new List<string> { "cutoff" },
                ["cv.assignable_targets"] = new List<string> { "cutoff" },
                ["cv.in1_range"] = 5,
                ["cv.in2_range"] = 5
            }
        };
    }

    public static Dictionary<string, object> GetSection(Dictionary<string, object> patch, string section)
    {
        return patch[section] as Dictionary<string, object>
               ?? throw new InvalidOperationException($"Section '{section}' was not found in the patch payload.");
    }
}
