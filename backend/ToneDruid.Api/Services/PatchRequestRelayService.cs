using System.Diagnostics;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Options;
using ToneDruid.Api.Models;
using ToneDruid.Api.Options;

namespace ToneDruid.Api.Services;

public sealed class PatchRequestRelayService
{
    private const string SystemPrompt = "You are Tone Druid. Reply conversationally to the given prompt.";

    private readonly OpenAIClient _client;
    private readonly AzureOpenAIOptions _options;
    private readonly ILogger<PatchRequestRelayService> _logger;
    private readonly TelemetryClient? _telemetry;

    public PatchRequestRelayService(
        OpenAIClient client,
        IOptions<AzureOpenAIOptions> options,
        ILogger<PatchRequestRelayService> logger,
        TelemetryClient? telemetry = null)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
        _telemetry = telemetry;
    }

    public async Task<PatchResponseDto> RelayAsync(
        string prompt,
        string clientRequestId,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Deployment))
        {
            throw new InvalidOperationException("AzureOpenAI deployment name is not configured.");
        }

        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        TrackEvent("patch_request_started", requestId, clientRequestId, startedAt, null);

        _logger.LogInformation(
            "Relaying prompt for RequestId {RequestId} and ClientRequestId {ClientRequestId}",
            requestId,
            clientRequestId);

        try
        {
            var chatOptions = new ChatCompletionsOptions
            {
                Messages =
                {
                    new ChatRequestSystemMessage(SystemPrompt),
                    new ChatRequestUserMessage(prompt)
                }
            };

            Response<ChatCompletions> response = await _client.GetChatCompletionsAsync(
                _options.Deployment,
                chatOptions,
                cancellationToken);

            string responseText = response.Value.Choices
                .SelectMany(choice => choice.Message.Content)
                .OfType<ChatMessageTextContentItem>()
                .Select(item => item.Text)
                .FirstOrDefault() ?? string.Empty;

            stopwatch.Stop();
            TrackSuccess(response.Value.Usage, requestId, clientRequestId, startedAt, stopwatch.Elapsed);

            _logger.LogInformation(
                "Received response for RequestId {RequestId} (ClientRequestId {ClientRequestId}) in {ElapsedMilliseconds} ms. Tokens: prompt={PromptTokens}, completion={CompletionTokens}, total={TotalTokens}",
                requestId,
                clientRequestId,
                stopwatch.ElapsedMilliseconds,
                response.Value.Usage?.PromptTokens ?? 0,
                response.Value.Usage?.CompletionTokens ?? 0,
                response.Value.Usage?.TotalTokens ?? 0);

            return new PatchResponseDto(
                prompt,
                responseText,
                requestId.ToString(),
                clientRequestId,
                DateTimeOffset.UtcNow);
        }
        catch (RequestFailedException ex)
        {
            stopwatch.Stop();
            TrackFailure(ex, requestId, clientRequestId, startedAt, stopwatch.Elapsed);
            _logger.LogError(ex, "Azure OpenAI request failed for RequestId {RequestId} and ClientRequestId {ClientRequestId}", requestId, clientRequestId);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TrackFailure(ex, requestId, clientRequestId, startedAt, stopwatch.Elapsed);
            _logger.LogError(ex, "Unhandled error relaying prompt for RequestId {RequestId} and ClientRequestId {ClientRequestId}", requestId, clientRequestId);
            throw;
        }
    }

    private void TrackSuccess(CompletionsUsage? usage, Guid requestId, string clientRequestId, DateTimeOffset startedAt, TimeSpan duration)
    {
        TrackEvent("patch_request_succeeded", requestId, clientRequestId, startedAt, duration, usage);
    }

    private void TrackFailure(Exception ex, Guid requestId, string clientRequestId, DateTimeOffset startedAt, TimeSpan duration)
    {
        TrackEvent("patch_request_failed", requestId, clientRequestId, startedAt, duration, null, ex);
    }

    private void TrackEvent(
        string eventName,
        Guid requestId,
        string clientRequestId,
        DateTimeOffset startedAt,
        TimeSpan? duration,
        CompletionsUsage? usage = null,
        Exception? exception = null)
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

        if (exception is not null)
        {
            telemetry.Properties["exceptionType"] = exception.GetType().Name;
        }

        _telemetry.TrackEvent(telemetry);
    }
}
