namespace ToneDruid.Api.Agents;

public sealed class PatchSuggestionValidationException : Exception
{
    public PatchSuggestionValidationException(string message, string? rawContent, Exception? innerException = null)
        : base(message, innerException)
    {
        RawContent = rawContent;
    }

    public string? RawContent { get; }
}
