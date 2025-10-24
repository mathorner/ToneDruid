namespace ToneDruid.Api.Models;

public sealed record PatchResponseDto(
    string Prompt,
    string Response,
    string RequestId,
    string ClientRequestId,
    DateTimeOffset GeneratedAtUtc
);
