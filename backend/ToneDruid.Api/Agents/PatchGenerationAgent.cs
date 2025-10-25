using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    private readonly JsonSerializerOptions _lenientSerializerOptions;
    private ChatCompletionsResponseFormat? _responseFormat;

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
        _lenientSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
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

        var chatOptions = CreateChatOptions(systemPrompt, prompt, _responseFormat);

        Response<ChatCompletions> response;
        try
        {
            response = await _client.GetChatCompletionsAsync(chatOptions, cancellationToken);
        }
        catch (RequestFailedException ex) when (ShouldFallbackToJsonFormat(ex) && !ReferenceEquals(_responseFormat, ChatCompletionsResponseFormat.JsonObject))
        {
            _logger.LogInformation("json_schema response format not supported by the current Azure OpenAI deployment. Falling back to default JSON parsing.");
            var fallbackOptions = CreateChatOptions(systemPrompt, prompt, ChatCompletionsResponseFormat.JsonObject);
            _responseFormat = ChatCompletionsResponseFormat.JsonObject;
            response = await _client.GetChatCompletionsAsync(fallbackOptions, cancellationToken);
        }

        string rawContent = response.Value.Choices
            .Select(choice => choice.Message.Content)
            .FirstOrDefault(content => !string.IsNullOrWhiteSpace(content))
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(rawContent))
        {
            throw new PatchSuggestionValidationException("Model response was empty.", rawContent);
        }

        var payload = TryDeserializePatchSuggestion(rawContent, out var deserializationError);

        if (payload is null)
        {
            if (deserializationError is not null)
            {
                _logger.LogWarning(deserializationError, "Failed to parse model response as PatchSuggestion JSON.");
            }
            throw new PatchSuggestionValidationException("Invalid JSON returned by model.", rawContent, deserializationError);
        }

        var normalized = NormalizeSuggestion(payload, prompt);

        ValidateSuggestion(normalized);

        var resolvedModel = response.Value.Model ?? normalized.Model ?? "unknown";

        var enriched = new PatchSuggestionDraft
        {
            Prompt = prompt,
            Summary = normalized.Summary,
            Controls = normalized.Controls,
            Reasoning = normalized.Reasoning,
            RequestId = normalized.RequestId,
            ClientRequestId = normalized.ClientRequestId,
            GeneratedAtUtc = normalized.GeneratedAtUtc,
            Model = resolvedModel
        };

        return (enriched, response.Value.Usage, resolvedModel, rawContent);
    }

    private PatchSuggestionDraft NormalizeSuggestion(PatchSuggestionDraft suggestion, string fallbackPrompt)
    {
        var controls = suggestion.Controls?
            .Select(NormalizeControl)
            .Where(control => control is not null)
            .Cast<PatchControlDto>()
            .ToList()
            ?? new List<PatchControlDto>();

        var reasoning = NormalizeReasoning(suggestion.Reasoning);

        return new PatchSuggestionDraft
        {
            Prompt = string.IsNullOrWhiteSpace(suggestion.Prompt) ? fallbackPrompt : suggestion.Prompt,
            Summary = string.IsNullOrWhiteSpace(suggestion.Summary) ? "Model response did not include a summary." : suggestion.Summary!,
            Controls = controls,
            Reasoning = reasoning,
            RequestId = suggestion.RequestId,
            ClientRequestId = suggestion.ClientRequestId,
            GeneratedAtUtc = suggestion.GeneratedAtUtc,
            Model = suggestion.Model
        };
    }

    private PatchControlDto? NormalizeControl(PatchControlDto control)
    {
        if (string.IsNullOrWhiteSpace(control.Id))
        {
            return null;
        }

        var catalogControl = _catalog.GetControlById(control.Id);

        var label = !string.IsNullOrWhiteSpace(control.Label)
            ? control.Label!
            : catalogControl?.Label ?? control.Id;

        var group = !string.IsNullOrWhiteSpace(control.Group)
            ? control.Group!
            : catalogControl?.GroupLabel ?? "General";

        var valueType = !string.IsNullOrWhiteSpace(control.ValueType)
            ? control.ValueType
            : catalogControl?.ValueType switch
            {
                VoiceParameterValueType.Boolean => "boolean",
                VoiceParameterValueType.Enumeration => "enumeration",
                VoiceParameterValueType.Continuous => "continuous",
                _ => "continuous"
            };

        var range = control.Range;
        if (range is null && catalogControl?.Range is not null)
        {
            range = new PatchControlRangeDto
            {
                Min = catalogControl.Range.Min,
                Max = catalogControl.Range.Max,
                Unit = catalogControl.Range.Unit
            };
        }

        var allowedValues = control.AllowedValues ?? catalogControl?.AllowedValues;

        var explanation = string.IsNullOrWhiteSpace(control.Explanation)
            ? ""
            : control.Explanation!;

        var confidence = string.IsNullOrWhiteSpace(control.Confidence)
            ? "medium"
            : control.Confidence!.ToLowerInvariant();

        return new PatchControlDto
        {
            Id = control.Id,
            Label = label,
            Group = group,
            Value = control.Value,
            ValueType = valueType,
            Range = range,
            AllowedValues = allowedValues,
            Explanation = explanation,
            Confidence = confidence
        };
    }

    private static PatchReasoningDto NormalizeReasoning(PatchReasoningDto? reasoning)
    {
        var intentSummary = string.IsNullOrWhiteSpace(reasoning?.IntentSummary)
            ? "Model did not provide an intent summary."
            : reasoning!.IntentSummary;

        var notes = reasoning?.SoundDesignNotes?
                .Where(note => !string.IsNullOrWhiteSpace(note))
                .Select(note => note!.Trim())
                .ToList()
            ?? new List<string>();

        if (notes.Count == 0)
        {
            notes.Add("Model did not provide detailed sound design notes.");
        }

        var assumptions = reasoning?.Assumptions?
                .Where(assumption => !string.IsNullOrWhiteSpace(assumption))
                .Select(assumption => assumption!.Trim())
                .ToArray()
            ?? Array.Empty<string>();

        return new PatchReasoningDto
        {
            IntentSummary = intentSummary,
            SoundDesignNotes = notes,
            Assumptions = assumptions
        };
    }

    private PatchSuggestionDraft? TryDeserializePatchSuggestion(string rawContent, out Exception? error)
    {
        error = null;
        var candidates = new List<string>();

        var trimmed = rawContent.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            candidates.Add(trimmed);
        }

        if (TryExtractJsonCodeBlock(rawContent, out var codeBlock))
        {
            candidates.Add(codeBlock);
        }

        if (TryExtractFirstJsonObject(rawContent, out var objectSnippet))
        {
            candidates.Add(objectSnippet);
        }

        // Remove duplicates while preserving order.
        var uniqueCandidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            if (seen.Add(candidate))
            {
                uniqueCandidates.Add(candidate);
            }
        }

        foreach (var candidate in uniqueCandidates)
        {
            try
            {
                return JsonSerializer.Deserialize<PatchSuggestionDraft>(candidate, _serializerOptions);
            }
            catch (JsonException ex)
            {
                error = ex;
            }
        }

        foreach (var candidate in uniqueCandidates)
        {
            try
            {
                return JsonSerializer.Deserialize<PatchSuggestionDraft>(candidate, _lenientSerializerOptions);
            }
            catch (JsonException ex)
            {
                error = ex;
            }
        }

        return null;
    }

    private static bool TryExtractJsonCodeBlock(string content, out string json)
    {
        var start = content.IndexOf("```", StringComparison.Ordinal);
        while (start >= 0)
        {
            var languageEnd = content.IndexOf('\n', start + 3);
            if (languageEnd < 0)
            {
                break;
            }

            var language = content.Substring(start + 3, languageEnd - (start + 3)).Trim().ToLowerInvariant();

            var end = content.IndexOf("```", languageEnd + 1, StringComparison.Ordinal);
            if (end < 0)
            {
                break;
            }

            var block = content.Substring(languageEnd + 1, end - (languageEnd + 1)).Trim();
            if (!string.IsNullOrEmpty(block) && (language.Length == 0 || language.Contains("json", StringComparison.Ordinal)))
            {
                json = block;
                return true;
            }

            start = content.IndexOf("```", end + 3, StringComparison.Ordinal);
        }

        json = string.Empty;
        return false;
    }

    private static bool TryExtractFirstJsonObject(string content, out string json)
    {
        var span = content.AsSpan();
        var index = span.IndexOf('{');
        if (index < 0)
        {
            json = string.Empty;
            return false;
        }

        var depth = 0;
        var inString = false;
        var escape = false;
        var startIndex = -1;

        for (var i = index; i < span.Length; i++)
        {
            var ch = span[i];

            if (inString)
            {
                if (ch == '\\' && !escape)
                {
                    escape = true;
                    continue;
                }

                if (ch == '"' && !escape)
                {
                    inString = false;
                }

                escape = false;
                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '{')
            {
                if (depth == 0)
                {
                    startIndex = i;
                }

                depth++;
                continue;
            }

            if (ch == '}')
            {
                depth--;
                if (depth == 0 && startIndex >= 0)
                {
                    json = span.Slice(startIndex, i - startIndex + 1).ToString().Trim();
                    return true;
                }
            }
        }

        json = string.Empty;
        return false;
    }

    private ChatCompletionsOptions CreateChatOptions(string systemPrompt, string userPrompt, ChatCompletionsResponseFormat? responseFormat)
    {
        var options = new ChatCompletionsOptions
        {
            DeploymentName = _options.Deployment,
            Temperature = 0.4f
        };

        if (responseFormat is not null)
        {
            options.ResponseFormat = responseFormat;
        }

        options.Messages.Add(new ChatRequestSystemMessage(systemPrompt));
        options.Messages.Add(new ChatRequestUserMessage(userPrompt));

        return options;
    }

    private static bool ShouldFallbackToJsonFormat(RequestFailedException ex)
    {
        return ex.Status == 400
            && ex.Message.Contains("response_format value as json_schema", StringComparison.OrdinalIgnoreCase);
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

        var jsonSchemaPayload = new
        {
            name = "patch_suggestion",
            schema,
            strict = true
        };

        var responseFormatAssembly = typeof(ChatCompletionsResponseFormat).Assembly;
        var jsonFormatType = responseFormatAssembly.GetType("Azure.AI.OpenAI.ChatCompletionsJsonResponseFormat");
        if (jsonFormatType is not null)
        {
            var ctor = jsonFormatType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                new[] { typeof(string), typeof(IDictionary<string, BinaryData>) },
                modifiers: null);

            if (ctor is not null)
            {
                var instance = ctor.Invoke(new object?[]
                {
                    "json_schema",
                    new Dictionary<string, BinaryData>
                    {
                        ["json_schema"] = BinaryData.FromObjectAsJson(jsonSchemaPayload)
                    }
                });

                if (instance is ChatCompletionsResponseFormat responseFormat)
                {
                    return responseFormat;
                }
            }
        }

        return ChatCompletionsResponseFormat.JsonObject;
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
