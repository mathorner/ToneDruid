namespace MinilogueXdValidation.Api.Services.Knowledge;

public interface IDocumentParser
{
    Task<DocumentParseResult> ParseAsync(Stream documentStream, CancellationToken cancellationToken);
}
