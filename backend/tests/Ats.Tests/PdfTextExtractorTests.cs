using Ats.Infrastructure.Services.Pdf;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Ats.Tests;

public class PdfTextExtractorTests
{
    [Fact]
    public async Task ExtractText_ReturnsContentFromEveryPage()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        builder.AddPage(PageSize.A4).AddText("Candidate profile: C# and PostgreSQL", 12, new PdfPoint(50, 780), font);
        builder.AddPage(PageSize.A4).AddText("Education: Computer Science", 12, new PdfPoint(50, 780), font);
        using var stream = new MemoryStream(builder.Build());

        var text = await new PdfPigTextExtractor().ExtractTextAsync(stream);

        Assert.Contains("Candidate profile: C# and PostgreSQL", text);
        Assert.Contains("Education: Computer Science", text);
    }
}
