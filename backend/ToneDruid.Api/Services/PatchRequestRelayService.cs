using System.Diagnostics;
using System.Linq;
using Azure;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using ToneDruid.Api.Agents;
using ToneDruid.Api.Models;

namespace ToneDruid.Api.Services;

public sealed class PatchRequestRelayService
{
    private readonly PatchGenerationAgent _agent;
    private readonly ILogger<PatchRequestRelayService> _logger;
    private readonly TelemetryClient? _telemetry;

    public PatchRequestRelayService(
        PatchGenerationAgent agent,
        ILogger<PatchRequestRelayService> logger,
        TelemetryClient? telemetry = null)
    {
        _agent = agent;
        _logger = logger;
        _telemetry = telemetry;
    }

    public async Task<PatchSuggestionDto> RelayAsync(
        string prompt,
        string clientRequestId,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        TrackEvent("patch_request_started", requestId, clientRequestId, startedAt, null);

        _logger.LogInformation(
            "Relaying prompt for RequestId {RequestId} and ClientRequestId {ClientRequestId}",
            requestId,
            clientRequestId);

        try
        {
            var (draft, usage, model, rawContent) = await _agent.GenerateAsync(prompt, cancellationToken);

            stopwatch.Stop();

            var generatedAt = DateTimeOffset.UtcNow;

            var reasoning = new PatchReasoningDto
            {
                IntentSummary = draft.Reasoning.IntentSummary,
                SoundDesignNotes = draft.Reasoning.SoundDesignNotes?.Where(note => !string.IsNullOrWhiteSpace(note)).ToList()
                    ?? Array.Empty<string>(),
                Assumptions = draft.Reasoning.Assumptions?.Where(assumption => !string.IsNullOrWhiteSpace(assumption)).ToList()
                    ?? Array.Empty<string>()
            };

            var suggestion = new PatchSuggestionDto
            {
                Prompt = prompt,
                Summary = draft.Summary,
                Controls = draft.Controls,
                Reasoning = reasoning,
                RequestId = requestId.ToString(),
                ClientRequestId = clientRequestId,
                GeneratedAtUtc = generatedAt.ToString("O"),
                Model = model ?? "unknown"
            };

            TrackSuccess(usage, suggestion, requestId, clientRequestId, startedAt, stopwatch.Elapsed, rawContent);

            _logger.LogInformation(
                "Received structured suggestion for RequestId {RequestId} (ClientRequestId {ClientRequestId}) in {ElapsedMilliseconds} ms. Tokens: prompt={PromptTokens}, completion={CompletionTokens}, total={TotalTokens}",
                requestId,
                clientRequestId,
                stopwatch.ElapsedMilliseconds,
                usage?.PromptTokens ?? 0,
                usage?.CompletionTokens ?? 0,
                usage?.TotalTokens ?? 0);

            return suggestion;
        }
        catch (PatchSuggestionValidationException ex)
        {
            stopwatch.Stop();
            TrackFailure(ex, requestId, clientRequestId, startedAt, stopwatch.Elapsed, ex.RawContent);
            _logger.LogWarning(
                ex,
                "Model response failed validation for RequestId {RequestId} and ClientRequestId {ClientRequestId}",
                requestId,
                clientRequestId);
            throw;
        }
        catch (RequestFailedException ex)
        {
            stopwatch.Stop();
            TrackFailure(ex, requestId, clientRequestId, startedAt, stopwatch.Elapsed, null);
            _logger.LogError(ex, "Azure OpenAI request failed for RequestId {RequestId} and ClientRequestId {ClientRequestId}", requestId, clientRequestId);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TrackFailure(ex, requestId, clientRequestId, startedAt, stopwatch.Elapsed, null);
            _logger.LogError(ex, "Unhandled error relaying prompt for RequestId {RequestId} and ClientRequestId {ClientRequestId}", requestId, clientRequestId);
            throw;
        }
    }

    private void TrackSuccess(
        CompletionsUsage? usage,
        PatchSuggestionDto suggestion,
        Guid requestId,
        string clientRequestId,
        DateTimeOffset startedAt,
        TimeSpan duration,
        string rawContent)
    {
        var properties = new Dictionary<string, string>
        {
            ["model"] = suggestion.Model,
            ["controlCount"] = suggestion.Controls.Count.ToString(),
            ["hasReasoning"] = (suggestion.Reasoning.SoundDesignNotes.Count > 0).ToString(),
            ["rawLength"] = rawContent.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        var confidenceBreakdown = suggestion.Controls
            .GroupBy(c => c.Confidence.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var kvp in confidenceBreakdown)
        {
            properties[$"confidence_{kvp.Key}"] = kvp.Value.ToString();
        }

        var measurements = new Dictionary<string, double>
        {
            ["controlCount"] = suggestion.Controls.Count,
            ["soundDesignNotes"] = suggestion.Reasoning.SoundDesignNotes.Count
        };

        TrackEvent("patch_request_succeeded", requestId, clientRequestId, startedAt, duration, usage, null, properties, measurements);
    }

    private void TrackFailure(Exception ex, Guid requestId, string clientRequestId, DateTimeOffset startedAt, TimeSpan duration, string? rawContent)
    {
        var properties = new Dictionary<string, string?>
        {
            ["exceptionType"] = ex.GetType().Name,
            ["rawResponse"] = string.IsNullOrEmpty(rawContent) ? null : Truncate(rawContent, 2048)
        };

        TrackEvent("patch_request_failed", requestId, clientRequestId, startedAt, duration, null, ex, properties, null);
    }

    private void TrackEvent(
        string eventName,
        Guid requestId,
        string clientRequestId,
        DateTimeOffset startedAt,
        TimeSpan? duration,
        CompletionsUsage? usage = null,
        Exception? exception = null,
        IDictionary<string, string?>? additionalProperties = null,
        IDictionary<string, double>? measurements = null)
    {
        if (_telemetry is null)
        {
            return;
        }

        var telemetry = new EventTelemetry(eventName)
        {
            Timestamp = startedAt
        };

        telemetry.Properties[nameof(requestId)] = requestId.ToString();
        telemetry.Properties[nameof(clientRequestId)] = clientRequestId;
        if (duration.HasValue)
        {
            telemetry.Metrics["latencyMs"] = duration.Value.TotalMilliseconds;
        }

        if (usage is not null)
        {
            telemetry.Metrics[nameof(CompletionsUsage.PromptTokens)] = usage.PromptTokens;
            telemetry.Metrics[nameof(CompletionsUsage.CompletionTokens)] = usage.CompletionTokens;
            telemetry.Metrics[nameof(CompletionsUsage.TotalTokens)] = usage.TotalTokens;
        }

        if (measurements is not null)
        {
            foreach (var (key, value) in measurements)
            {
                telemetry.Metrics[key] = value;
            }
        }

        if (exception is not null)
        {
            telemetry.Properties["exceptionType"] = exception.GetType().Name;
        }

        if (additionalProperties is not null)
        {
            foreach (var (key, value) in additionalProperties)
            {
                if (value is not null)
                {
                    telemetry.Properties[key] = value;
                }
            }
        }

        _telemetry.TrackEvent(telemetry);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
