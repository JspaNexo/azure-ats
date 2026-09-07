using FluentAssertions;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Ats.Infrastructure.Services.Pdf;
using Xunit;

namespace Ats.ArchitectureTests;

public class CvGenerationTests
{
    [Fact]
    public async Task GenerateSampleCandidateCvPdf_ShouldCreateValidPdf_AndExtractText()
    {
        // 1. Build a realistic Candidate CV PDF using PdfPig
        var builder = new PdfDocumentBuilder();
        var regularFont = builder.AddStandard14Font(Standard14Font.Helvetica);
        var boldFont = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        var page = builder.AddPage(PageSize.A4);

        // Header
        page.AddText("MATEO MORALES", 20, new PdfPoint(50, 780), boldFont);
        page.AddText("Senior .NET & Cloud Software Engineer", 13, new PdfPoint(50, 760), regularFont);
        page.AddText("Email: mateo.morales@example.com | Tel: +1 (809) 555-0199 | Santo Domingo, RD", 10, new PdfPoint(50, 742), regularFont);
        page.AddText("LinkedIn: linkedin.com/in/mateomorales | GitHub: github.com/mateomorales", 10, new PdfPoint(50, 728), regularFont);

        // Resumen
        page.AddText("PERFIL PROFESIONAL", 12, new PdfPoint(50, 695), boldFont);
        page.AddText("Ingeniero de software con mas de 6 anos de experiencia disenando y construyendo sistemas distribuidos,", 10, new PdfPoint(50, 675), regularFont);
        page.AddText("microservicios de alto rendimiento con ASP.NET Core, PostgreSQL, Docker y arquitecturas Cloud en Azure.", 10, new PdfPoint(50, 660), regularFont);
        page.AddText("Especializado en Clean Architecture, Domain-Driven Design (DDD), patrones de resiliencia y CI/CD.", 10, new PdfPoint(50, 645), regularFont);

        // Experiencia Laboral
        page.AddText("EXPERIENCIA LABORAL", 12, new PdfPoint(50, 615), boldFont);

        page.AddText("Senior Backend Engineer | InnovaTech Solutions (2021 - Presente)", 10, new PdfPoint(50, 595), boldFont);
        page.AddText("- Lidero el diseno de APIs RESTful y microservicios con .NET 8 / .NET 9 procesando mas de 5M req/dia.", 9, new PdfPoint(60, 580), regularFont);
        page.AddText("- Optimizo modelos de datos y consultas complejas en PostgreSQL con EF Core y JSONB, reduciendo latencia un 35%.", 9, new PdfPoint(60, 567), regularFont);
        page.AddText("- Implemento integracion de pipelines con Docker, Kubernetes y Azure DevOps.", 9, new PdfPoint(60, 554), regularFont);

        page.AddText("Full Stack Developer | Caribe Software Labs (2018 - 2021)", 10, new PdfPoint(50, 530), boldFont);
        page.AddText("- Desarrollo de aplicaciones web escalables con C#, ASP.NET MVC, React y SQL Server.", 9, new PdfPoint(60, 515), regularFont);
        page.AddText("- Diseno de autenticacion OAuth2 / OpenID Connect y refactorizacion hacia Clean Code.", 9, new PdfPoint(60, 502), regularFont);

        // Habilidades Tecnicas
        page.AddText("HABILIDADES TECNICAS", 12, new PdfPoint(50, 470), boldFont);
        page.AddText("Lenguajes y Frameworks: C#, .NET 8/9/10, ASP.NET Core, Entity Framework Core, React, TypeScript", 9, new PdfPoint(50, 452), regularFont);
        page.AddText("Bases de Datos: PostgreSQL, SQL Server, Redis, MongoDB", 9, new PdfPoint(50, 439), regularFont);
        page.AddText("DevOps y Cloud: Docker, Kubernetes, Azure, Git, GitHub Actions, Linux", 9, new PdfPoint(50, 426), regularFont);
        page.AddText("Metodologias y Buenas Practicas: Clean Architecture, SOLID, TDD, Scrum, Domain-Driven Design", 9, new PdfPoint(50, 413), regularFont);

        // Educacion y Certificaciones
        page.AddText("EDUCACION Y CERTIFICACIONES", 12, new PdfPoint(50, 380), boldFont);
        page.AddText("Ingenieria en Sistemas Computacionales | Instituto Tecnologico de Santo Domingo (INTEC) - 2018", 9, new PdfPoint(50, 362), regularFont);
        page.AddText("Microsoft Certified: Azure Solutions Architect Expert (2023)", 9, new PdfPoint(50, 349), regularFont);
        page.AddText("Microsoft Certified: Azure Developer Associate (2022)", 9, new PdfPoint(50, 336), regularFont);

        // Idiomas
        page.AddText("IDIOMAS", 12, new PdfPoint(50, 305), boldFont);
        page.AddText("Espanol: Nativo | Ingles: C1 Avanzado (Fluido profesional)", 9, new PdfPoint(50, 287), regularFont);

        byte[] pdfBytes = builder.Build();

        // Save PDF to root folder for API test upload
        string outputPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "sample_cv_mateo_morales.pdf");
        outputPath = Path.GetFullPath(outputPath);
        await File.WriteAllBytesAsync(outputPath, pdfBytes);

        File.Exists(outputPath).Should().BeTrue();
        pdfBytes.Length.Should().BeGreaterThan(500);

        // 2. Verify text extraction with PdfPigTextExtractor
        using var stream = new MemoryStream(pdfBytes);
        var extractor = new PdfPigTextExtractor();
        string extractedText = await extractor.ExtractTextAsync(stream);

        extractedText.Should().Contain("MATEO MORALES");
        extractedText.Should().Contain("Senior .NET & Cloud Software Engineer");
        extractedText.Should().Contain("PostgreSQL");
        extractedText.Should().Contain("Clean Architecture");
    }
}
