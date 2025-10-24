using System;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using ToneDruid.Api.Models;
using ToneDruid.Api.Options;
using ToneDruid.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var endpoint = configuration["AzureOpenAI:Endpoint"];
    if (string.IsNullOrWhiteSpace(endpoint))
    {
        throw new InvalidOperationException("AzureOpenAI:Endpoint configuration value is required.");
    }

    var apiKey = configuration["AzureOpenAI:ApiKey"];
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        return new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    TokenCredential credential = new DefaultAzureCredential();
    return new OpenAIClient(new Uri(endpoint), credential);
});

builder.Services.AddScoped<PatchRequestRelayService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                return uri.IsLoopback ||
                       string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
            });
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("FrontendDev");

app.MapPost("/api/v1/patch-request", async (
    HttpContext context,
    PatchRequestDto requestDto,
    PatchRequestRelayService relayService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("PatchRequest");

    if (requestDto is null || string.IsNullOrWhiteSpace(requestDto.Prompt))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(PatchRequestDto.Prompt)] = new[] { "Prompt is required." }
        });
    }

    var prompt = requestDto.Prompt.Trim();
    if (prompt.Length == 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(PatchRequestDto.Prompt)] = new[] { "Prompt must contain non-whitespace characters." }
        });
    }

    if (prompt.Length > 500)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(PatchRequestDto.Prompt)] = new[] { "Prompt cannot exceed 500 characters." }
        });
    }

    var clientRequestId = context.Request.Headers.TryGetValue("x-client-request-id", out var headerValue)
        ? headerValue.FirstOrDefault()
        : null;
    if (string.IsNullOrWhiteSpace(clientRequestId))
    {
        clientRequestId = Guid.NewGuid().ToString();
    }

    var requestId = Guid.NewGuid();
    context.Response.Headers["Request-Id"] = requestId.ToString();

    try
    {
        var response = await relayService.RelayAsync(prompt, clientRequestId, requestId, cancellationToken);
        return Results.Ok(response);
    }
    catch (RequestFailedException ex)
    {
        logger.LogError(ex, "Azure OpenAI returned an error for RequestId {RequestId} and ClientRequestId {ClientRequestId}", requestId, clientRequestId);
        return Results.Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "Upstream service error",
            detail: "The AI service was unable to complete the request at this time. Please try again later.",
            extensions: new Dictionary<string, object?>
            {
                ["requestId"] = requestId.ToString(),
                ["clientRequestId"] = clientRequestId
            });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled error processing prompt for RequestId {RequestId} and ClientRequestId {ClientRequestId}", requestId, clientRequestId);
        return Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Unexpected error",
            detail: "An unexpected error occurred while processing the request.",
            extensions: new Dictionary<string, object?>
            {
                ["requestId"] = requestId.ToString(),
                ["clientRequestId"] = clientRequestId
            });
    }
});

app.Run();
