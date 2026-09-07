using System.Text;
using UglyToad.PdfPig;
using Ats.Application.Common.Interfaces;

namespace Ats.Infrastructure.Services.Pdf;

public class PdfPigTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(pdfStream);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            string pageText = page.Text;
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                sb.AppendLine(pageText);
            }
        }

        return Task.FromResult(sb.ToString().Trim());
    }
}

