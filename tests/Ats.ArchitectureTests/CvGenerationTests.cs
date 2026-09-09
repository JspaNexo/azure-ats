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

    [Fact]
    public async Task GenerateSeedCandidatePdfs_ShouldCreateAllSixCvPdfs()
    {
        var candidates = new (string FileName, string FullName, string Role, string Email, int Years, string Summary, string Skills, string Exp1, string Exp2)[]
        {
            (
                "CV_Sofia_Valenzuela_TechLead.pdf",
                "SOFIA VALENZUELA NAVARRO",
                "Lead Cloud Architect & Tech Lead",
                "sofia.valenzuela@techlead.dev",
                9,
                "Arquitecta de Software y Tech Lead con 9 anos de experiencia liderando sistemas distribuidos en .NET, Kubernetes y Azure.",
                "C#, .NET Core, Azure, Kubernetes, Kafka, Clean Architecture, DDD, Docker",
                "Lead Cloud Architect | Innovatech Global (2021 - Presente) - Liderazgo de arquitectura multicloud y microservicios resilientes.",
                "Senior Backend Tech Lead | Fintech Solutions (2018 - 2021) - Diseno de pasarelas de pago y migracion a contenedores."
            ),
            (
                "CV_Carlos_Mendoza_Backend.pdf",
                "CARLOS MENDOZA RIVAS",
                "Senior Backend Engineer",
                "carlos.mendoza.dev@gmail.com",
                6,
                "Ingeniero de backend especializado en microservicios transaccionales de alto rendimiento con C#, ASP.NET Core y PostgreSQL.",
                "C#, .NET 10, PostgreSQL, Entity Framework Core, Redis, Docker, Microservicios",
                "Senior Backend Developer | Digital Logistics (2022 - Presente) - Optimizacion de queries SQL y APIs transaccionales.",
                "Backend Software Engineer | Soluciones Transaccionales (2019 - 2022) - Servicios de integracion con colas RabbitMQ."
            ),
            (
                "CV_Valeria_Herrera_Fullstack.pdf",
                "VALERIA HERRERA SALAS",
                "Fullstack Developer (React & .NET)",
                "valeria.herrera@gmail.com",
                4,
                "Desarrolladora Fullstack apasionada por interfaces web fluidas y APIs robustas con React, TypeScript y .NET Core.",
                "React, TypeScript, Tailwind CSS, C#, .NET Core, PostgreSQL, REST APIs",
                "Fullstack Engineer | Frontend Innovations (2022 - Presente) - Diseno de dashboards reactivos y consumo de APIs.",
                "Software Developer | WebCraft Studio (2020 - 2022) - Mantenimiento de modulos web y bases de datos relacionales."
            ),
            (
                "CV_Alejandro_Gomez_DevOps.pdf",
                "ALEJANDRO GOMEZ DUARTE",
                "DevOps & Cloud Infrastructure Lead",
                "alejandro.gomez@cloudinfra.io",
                8,
                "Ingeniero de infraestructura cloud y DevOps con amplia trayectoria en automatizacion, Terraform, Kubernetes y CI/CD.",
                "Terraform, Kubernetes, Docker, AWS, Azure DevOps, Linux, Prometheus, Grafana",
                "Principal DevOps Engineer | CloudScale Tech (2021 - Presente) - Gestion de clusters EKS/AKS y pipelines GitOps.",
                "Site Reliability Engineer | MediaStream Networks (2018 - 2021) - Monitoreo proactivo y reduccion de caidas al 99.99%."
            ),
            (
                "CV_Mariana_Pineda_DataEngineer.pdf",
                "MARIANA PINEDA RUIZ",
                "Senior Data Engineer",
                "mariana.pineda.data@outlook.com",
                5,
                "Ingeniera de datos especializada en pipelines ETL/ELT escalables, Apache Spark, Airflow, BigQuery y modelado analitico.",
                "Python, SQL, Apache Spark, Apache Airflow, BigQuery, PostgreSQL, Databricks",
                "Senior Data Engineer | Analytics Core (2022 - Presente) - Orquestacion de pipelines para ingesta de terabytes diarios.",
                "Data Developer | DataInsight Lab (2020 - 2022) - Construccion de Data Marts y visualizaciones analiticas."
            ),
            (
                "CV_David_Rangel_QA_Automation.pdf",
                "DAVID RANGEL OSORIO",
                "QA Automation & Reliability Engineer",
                "david.rangel.qa@techqa.dev",
                5,
                "Ingeniero de aseguramiento de calidad enfocado en pruebas automatizadas E2E, integracion continua y rendimiento.",
                "Playwright, Cypress, C#, Selenium, Postman, CI/CD, JMeter, K6",
                "Lead QA Automation | QualityFirst Systems (2022 - Presente) - Frameworks de automatizacion cross-browser para web y APIs.",
                "QA Engineer | Caribbean Software House (2020 - 2022) - Diseno de casos de prueba y suites de regresion automatizadas."
            )
        };

        // Determine storage directories
        var storageDirs = new List<string>
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "storage")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "storage")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "backend", "src", "Ats.Api", "bin", "Debug", "net10.0", "storage"))
        };

        foreach (var dir in storageDirs)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        foreach (var c in candidates)
        {
            var builder = new PdfDocumentBuilder();
            var regularFont = builder.AddStandard14Font(Standard14Font.Helvetica);
            var boldFont = builder.AddStandard14Font(Standard14Font.HelveticaBold);

            var page = builder.AddPage(PageSize.A4);

            // Header
            page.AddText(c.FullName, 18, new PdfPoint(50, 780), boldFont);
            page.AddText(c.Role, 12, new PdfPoint(50, 760), regularFont);
            page.AddText($"Email: {c.Email} | Experiencia: {c.Years} anos", 10, new PdfPoint(50, 742), regularFont);

            // Perfil
            page.AddText("PERFIL PROFESIONAL", 11, new PdfPoint(50, 710), boldFont);
            page.AddText(c.Summary, 9, new PdfPoint(50, 692), regularFont);

            // Habilidades
            page.AddText("COMPETENCIAS Y TECNOLOGIAS", 11, new PdfPoint(50, 660), boldFont);
            page.AddText(c.Skills, 9, new PdfPoint(50, 642), regularFont);

            // Experiencia
            page.AddText("TRAYECTORIA PROFESIONAL", 11, new PdfPoint(50, 610), boldFont);
            page.AddText(c.Exp1, 9, new PdfPoint(50, 592), regularFont);
            page.AddText(c.Exp2, 9, new PdfPoint(50, 565), regularFont);

            // Educacion
            page.AddText("FORMACION ACADEMICA", 11, new PdfPoint(50, 530), boldFont);
            page.AddText("Grado en Ingenieria Informatica / Software - Graduacion con honores", 9, new PdfPoint(50, 512), regularFont);

            byte[] pdfBytes = builder.Build();

            foreach (var dir in storageDirs)
            {
                string targetPath = Path.Combine(dir, c.FileName);
                await File.WriteAllBytesAsync(targetPath, pdfBytes);
                File.Exists(targetPath).Should().BeTrue();
            }
        }
    }
}
