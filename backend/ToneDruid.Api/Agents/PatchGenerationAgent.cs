using System.Linq;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ToneDruid.Api.Models;
using ToneDruid.Api.Models.VoiceParameters;
using ToneDruid.Api.Options;
using ToneDruid.Api.Services;

namespace ToneDruid.Api.Agents;

public sealed class PatchGenerationAgent
{
    private const int PromptSubsetLimitPerGroup = 8;

    private readonly OpenAIClient _client;
    private readonly AzureOpenAIOptions _options;
    private readonly IVoiceParameterCatalog _catalog;
    private readonly ILogger<PatchGenerationAgent> _logger;
    private readonly string _systemPromptTemplate;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ChatCompletionsResponseFormat _responseFormat;

    public PatchGenerationAgent(
        OpenAIClient client,
        IOptions<AzureOpenAIOptions> options,
        IVoiceParameterCatalog catalog,
        ILogger<PatchGenerationAgent> logger,
        IHostEnvironment environment)
    {
        _client = client;
        _options = options.Value;
        _catalog = catalog;
        _logger = logger;
        _systemPromptTemplate = LoadPromptTemplate(environment);
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false
        };
        _responseFormat = BuildResponseFormat();
    }

    public async Task<(PatchSuggestionDraft Suggestion, CompletionsUsage? Usage, string Model, string RawContent)> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Deployment))
        {
            throw new InvalidOperationException("AzureOpenAI deployment name is not configured.");
        }

        var systemPrompt = _systemPromptTemplate.Replace("{{CONTROL_CATALOG}}", _catalog.BuildPromptCatalog(PromptSubsetLimitPerGroup));

        var chatOptions = new ChatCompletionsOptions
        {
            DeploymentName = _options.Deployment,
            Temperature = 0.4f,
            ResponseFormat = _responseFormat
        };

        chatOptions.Messages.Add(new ChatRequestSystemMessage(systemPrompt));
        chatOptions.Messages.Add(new ChatRequestUserMessage(prompt));

        Response<ChatCompletions> response = await _client.GetChatCompletionsAsync(chatOptions, cancellationToken);

        string rawContent = response.Value.Choices
            .SelectMany(choice => choice.Message.Content)
            .OfType<ChatMessageTextContentItem>()
            .Select(item => item.Text)
            .FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(rawContent))
        {
            throw new PatchSuggestionValidationException("Model response was empty.", rawContent);
        }

        PatchSuggestionDraft? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PatchSuggestionDraft>(rawContent, _serializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse model response as PatchSuggestion JSON.");
            throw new PatchSuggestionValidationException("Invalid JSON returned by model.", rawContent, ex);
        }

        if (payload is null)
        {
            throw new PatchSuggestionValidationException("Model response did not contain a patch suggestion.", rawContent);
        }

        ValidateSuggestion(payload);

        var resolvedModel = response.Value.Model ?? payload.Model ?? "unknown";

        var enriched = new PatchSuggestionDraft
        {
            Prompt = prompt,
            Summary = payload.Summary,
            Controls = payload.Controls,
            Reasoning = payload.Reasoning,
            RequestId = payload.RequestId,
            ClientRequestId = payload.ClientRequestId,
            GeneratedAtUtc = payload.GeneratedAtUtc,
            Model = resolvedModel
        };

        return (enriched, response.Value.Usage, resolvedModel, rawContent);
    }

    private void ValidateSuggestion(PatchSuggestionDraft suggestion)
    {
        if (suggestion.Controls is null || suggestion.Controls.Count is < 5 or > 10)
        {
            throw new PatchSuggestionValidationException("Model produced an invalid number of controls.", null);
        }

        foreach (var control in suggestion.Controls)
        {
            var catalogControl = _catalog.GetControlById(control.Id);
            if (catalogControl is null)
            {
                throw new PatchSuggestionValidationException($"Control '{control.Id}' is not recognised in the catalog.", null);
            }

            ValidateValueType(control, catalogControl);
            ValidateConfidence(control);
            ValidateRange(control, catalogControl);
        }

        if (suggestion.Reasoning is null)
        {
            throw new PatchSuggestionValidationException("Reasoning block is missing.", null);
        }

        if (suggestion.Reasoning.SoundDesignNotes is null || suggestion.Reasoning.SoundDesignNotes.Count == 0)
        {
            throw new PatchSuggestionValidationException("Reasoning must include sound design notes.", null);
        }

        if (string.IsNullOrWhiteSpace(suggestion.Reasoning.IntentSummary))
        {
            throw new PatchSuggestionValidationException("Reasoning must include an intent summary.", null);
        }
    }

    private static void ValidateValueType(PatchControlDto control, VoiceParameter catalogControl)
    {
        var expectedType = catalogControl.ValueType switch
        {
            VoiceParameterValueType.Boolean => "boolean",
            VoiceParameterValueType.Enumeration => "enumeration",
            _ => "continuous"
        };

        if (!string.Equals(control.ValueType, expectedType, StringComparison.OrdinalIgnoreCase))
        {
            throw new PatchSuggestionValidationException(
                $"Control '{control.Id}' has mismatched value type. Expected {expectedType}.",
                null);
        }

        if (catalogControl.ValueType == VoiceParameterValueType.Enumeration)
        {
            if (catalogControl.AllowedValues is null || catalogControl.AllowedValues.Count == 0)
            {
                throw new PatchSuggestionValidationException(
                    $"Catalog entry for '{catalogControl.Id}' is missing allowed values.",
                    null);
            }

            if (control.AllowedValues is null || control.AllowedValues.Count == 0)
            {
                throw new PatchSuggestionValidationException(
                    $"Control '{control.Id}' must include allowedValues list.",
                    null);
            }
        }
    }

    private static void ValidateConfidence(PatchControlDto control)
    {
        var validConfidences = new[] { "low", "medium", "high" };
        if (!validConfidences.Contains(control.Confidence, StringComparer.OrdinalIgnoreCase))
        {
            throw new PatchSuggestionValidationException(
                $"Control '{control.Id}' has invalid confidence '{control.Confidence}'.",
                null);
        }
    }

    private static void ValidateRange(PatchControlDto control, VoiceParameter catalogControl)
    {
        if (catalogControl.Range is null)
        {
            return;
        }

        if (control.Value.ValueKind is not (JsonValueKind.Number or JsonValueKind.String))
        {
            return;
        }

        if (!control.Value.TryGetDouble(out var value) && !double.TryParse(control.Value.GetString(), out value))
        {
            return;
        }

        if (value < catalogControl.Range.Min || value > catalogControl.Range.Max)
        {
            throw new PatchSuggestionValidationException(
                $"Control '{control.Id}' value {value} is outside the allowed range {catalogControl.Range.Min}..{catalogControl.Range.Max}.",
                null);
        }
    }

    private static ChatCompletionsResponseFormat BuildResponseFormat()
    {
        var schema = new
        {
            type = "object",
            additionalProperties = false,
            required = new[]
            {
                "prompt",
                "summary",
                "controls",
                "reasoning",
                "requestId",
                "clientRequestId",
                "generatedAtUtc",
                "model"
            },
            properties = new Dictionary<string, object>
            {
                ["prompt"] = new { type = "string" },
                ["summary"] = new { type = "string" },
                ["requestId"] = new { type = "string" },
                ["clientRequestId"] = new { type = "string" },
                ["generatedAtUtc"] = new { type = "string", format = "date-time" },
                ["model"] = new { type = "string" },
                ["controls"] = new
                {
                    type = "array",
                    minItems = 5,
                    maxItems = 10,
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[]
                        {
                            "id",
                            "label",
                            "group",
                            "value",
                            "valueType",
                            "explanation",
                            "confidence"
                        },
                        properties = new Dictionary<string, object>
                        {
                            ["id"] = new { type = "string" },
                            ["label"] = new { type = "string" },
                            ["group"] = new { type = "string" },
                            ["valueType"] = new { type = "string", enumValues = new[] { "continuous", "enumeration", "boolean" } },
                            ["value"] = new { anyOf = new[] { new { type = "number" }, new { type = "string" } } },
                            ["explanation"] = new { type = "string" },
                            ["confidence"] = new { type = "string", enumValues = new[] { "low", "medium", "high" } },
                            ["allowedValues"] = new
                            {
                                type = "array",
                                items = new { type = "string" }
                            },
                            ["range"] = new
                            {
                                type = new[] { "object", "null" },
                                required = new[] { "min", "max" },
                                properties = new Dictionary<string, object>
                                {
                                    ["min"] = new { type = "number" },
                                    ["max"] = new { type = "number" },
                                    ["unit"] = new { type = new[] { "string", "null" } }
                                }
                            }
                        }
                    }
                },
                ["reasoning"] = new
                {
                    type = "object",
                    additionalProperties = false,
                    required = new[] { "intentSummary", "soundDesignNotes", "assumptions" },
                    properties = new Dictionary<string, object>
                    {
                        ["intentSummary"] = new { type = "string" },
                        ["soundDesignNotes"] = new { type = "array", items = new { type = "string" }, minItems = 1 },
                        ["assumptions"] = new { type = "array", items = new { type = "string" } }
                    }
                }
            }
        };

        return ChatCompletionsResponseFormat.CreateJsonSchema(
            "patch_suggestion",
            BinaryData.FromObjectAsJson(schema));
    }

    private static string LoadPromptTemplate(IHostEnvironment environment)
    {
        var templatePath = Path.Combine(environment.ContentRootPath, "Resources", "Prompts", "PatchGenerationSystemPrompt.txt");
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Prompt template not found at {templatePath}");
        }

        return File.ReadAllText(templatePath);
    }
}
