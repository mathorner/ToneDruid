using UglyToad.PdfPig;

namespace MinilogueXdValidation.Api.Services.Knowledge;

public sealed class PdfDocumentParser : IDocumentParser
{
    public Task<DocumentParseResult> ParseAsync(Stream documentStream, CancellationToken cancellationToken)
    {
        if (documentStream is null)
        {
            throw new ArgumentNullException(nameof(documentStream));
        }

        if (!documentStream.CanRead)
        {
            throw new ArgumentException("Document stream must be readable.", nameof(documentStream));
        }

        if (documentStream.CanSeek)
        {
            documentStream.Seek(0, SeekOrigin.Begin);
        }

        using var pdf = PdfDocument.Open(documentStream);
        var pages = new List<DocumentPage>(pdf.NumberOfPages);

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = page.Text ?? string.Empty;
            pages.Add(new DocumentPage(page.Number, text.Trim()));
        }

        return Task.FromResult(new DocumentParseResult(pages.Count, pages));
    }
}
