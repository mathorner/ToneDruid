namespace MinilogueXdValidation.Api.Services.Knowledge;

public sealed record DocumentParseResult(int PageCount, IReadOnlyList<DocumentPage> Pages);

public sealed record DocumentPage(int PageNumber, string Text);
