namespace MinilogueXdValidation.Api.Models;

public sealed record ValidationError(string Field, string? Value, string Message);

public sealed class PatchValidationResult
{
    private PatchValidationResult(bool isValid, IReadOnlyList<ValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public bool IsValid { get; }

    public IReadOnlyList<ValidationError> Errors { get; }

    public static PatchValidationResult Success() => new(true, Array.Empty<ValidationError>());

    public static PatchValidationResult Failure(IReadOnlyList<ValidationError> errors)
        => new(false, errors);
}
