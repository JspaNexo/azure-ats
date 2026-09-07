using System.IO;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace Ats.Tests;

public class GenerateSampleCvTest
{
    [Fact]
    public void GenerateNoExperienceCandidateCv()
    {
        var builder = new PdfDocumentBuilder();
        var regularFont = builder.AddStandard14Font(Standard14Font.Helvetica);
        var boldFont = builder.AddStandard14Font(Standard14Font.HelveticaBold);
        var obliqueFont = builder.AddStandard14Font(Standard14Font.HelveticaOblique);

        var page = builder.AddPage(PageSize.A4);

        // Header
        page.AddText("MATEO SILVA RIVAS", 18, new PdfPoint(50, 785), boldFont);
        page.AddText("Ingeniero de Software Trainee / Recien Graduado", 11, new PdfPoint(50, 768), boldFont);
        page.AddText("Correo: mateo.silva@ejemplo.com  |  Tel: +1 (809) 555-0199  |  Santo Domingo, Republica Dominicana", 9, new PdfPoint(50, 752), regularFont);
        page.AddText("GitHub: github.com/mateosilva  |  LinkedIn: linkedin.com/in/mateo-silva-dev", 9, new PdfPoint(50, 738), regularFont);

        // Line separator
        page.AddText("----------------------------------------------------------------------------------------------------------------------------------", 8, new PdfPoint(50, 725), regularFont);

        // Perfil Profesional
        int y = 705;
        page.AddText("PERFIL PROFESIONAL", 11, new PdfPoint(50, y), boldFont);
        y -= 16;
        page.AddText("Recien graduado de Ingenieria en Sistemas y Computacion con solida base teorica en algoritmos, estructuras", 9, new PdfPoint(50, y), regularFont);
        y -= 13;
        page.AddText("de datos, programacion orientada a objetos (POO) y diseno de bases de datos relacionales. Alta motivacion y", 9, new PdfPoint(50, y), regularFont);
        y -= 13;
        page.AddText("capacidad de aprendizaje rapido para incorporarse a equipos de desarrollo backend y fullstack. Experiencia", 9, new PdfPoint(50, y), regularFont);
        y -= 13;
        page.AddText("practica desarrollada mediante rigurosos proyectos academicos, desarrollo de software open source y ayudantia", 9, new PdfPoint(50, y), regularFont);
        y -= 13;
        page.AddText("docente universitaria en laboratorios de computacion. Interesado en vacantes de Software Engineer Trainee / Junior.", 9, new PdfPoint(50, y), regularFont);

        // Educacion
        y -= 24;
        page.AddText("EDUCACION Y FORMACION ACADEMICA", 11, new PdfPoint(50, y), boldFont);
        y -= 16;
        page.AddText("Grado en Ingenieria en Sistemas y Computacion (Honores Cum Laude)", 10, new PdfPoint(50, y), boldFont);
        y -= 13;
        page.AddText("Pontificia Universidad Catolica Madre y Maestra (PUCMM)  |  2021 - 2026", 9, new PdfPoint(50, y), obliqueFont);
        y -= 13;
        page.AddText("- Promedio Academico destacado: 3.85 / 4.0. Indice de excelencia academica.", 9, new PdfPoint(60, y), regularFont);
        y -= 13;
        page.AddText("- Tesis de Grado: 'Arquitectura de Software y Microservicios para Gestion de Catalogos Académicos'.", 9, new PdfPoint(60, y), regularFont);

        // Proyectos Academicos y Practicos (Sin Experiencia Formal)
        y -= 24;
        page.AddText("PROYECTOS ACADEMICOS Y EXPERIENCIA FORMATIVA", 11, new PdfPoint(50, y), boldFont);
        y -= 16;
        page.AddText("1. Sistema de Gestion Bibliotecaria Universitaria (Proyecto de Grado - 2025)", 10, new PdfPoint(50, y), boldFont);
        y -= 13;
        page.AddText("   Tecnologias: C#, .NET 8, ASP.NET Core Web API, Entity Framework Core, PostgreSQL, Git.", 9, new PdfPoint(50, y), obliqueFont);
        y -= 13;
        page.AddText("   - Diseno e implementacion de API RESTful con autenticacion basada en tokens JWT y arquitectura en capas.", 9, new PdfPoint(50, y), regularFont);
        y -= 13;
        page.AddText("   - Modelado de base de datos relacional en PostgreSQL con transaccionalidad y consultas LINQ optimizadas.", 9, new PdfPoint(50, y), regularFont);

        y -= 18;
        page.AddText("2. Plataforma Colaborativa de Tareas Escolares (Proyecto Final de Semestre - 2024)", 10, new PdfPoint(50, y), boldFont);
        y -= 13;
        page.AddText("   Tecnologias: JavaScript, TypeScript, React, Node.js, Express, Tailwind CSS.", 9, new PdfPoint(50, y), obliqueFont);
        y -= 13;
        page.AddText("   - Desarrollo de interfaz de usuario responsiva con React y diseno de componentes modulares.", 9, new PdfPoint(50, y), regularFont);
        y -= 13;
        page.AddText("   - Creacion de endpoints de backend para almacenamiento de notas y asignaciones escolares.", 9, new PdfPoint(50, y), regularFont);

        y -= 18;
        page.AddText("3. Ayudante de Catedra - Laboratorio de Algoritmos y Programacion (2024 - 2025)", 10, new PdfPoint(50, y), boldFont);
        y -= 13;
        page.AddText("   Departamento de Ingenieria de Sistemas, PUCMM", 9, new PdfPoint(50, y), obliqueFont);
        y -= 13;
        page.AddText("   - Asistencia y soporte docente a mas de 45 estudiantes en resolucion de problemas con C# y Python.", 9, new PdfPoint(50, y), regularFont);
        y -= 13;
        page.AddText("   - Revision de buenas practicas de desarrollo, control de versiones con Git y pruebas unitarias basicas.", 9, new PdfPoint(50, y), regularFont);

        // Competencias Tecnicas
        y -= 24;
        page.AddText("COMPETENCIAS TECNICAS Y HABILIDADES", 11, new PdfPoint(50, y), boldFont);
        y -= 15;
        page.AddText("- Lenguajes de Programacion: C#, JavaScript, TypeScript, Python basico, SQL.", 9, new PdfPoint(60, y), regularFont);
        y -= 13;
        page.AddText("- Frameworks y Librerias: .NET 8, ASP.NET Core, Entity Framework Core, React, Node.js.", 9, new PdfPoint(60, y), regularFont);
        y -= 13;
        page.AddText("- Bases de Datos: PostgreSQL, Microsoft SQL Server, diseno relacional y consultas SQL.", 9, new PdfPoint(60, y), regularFont);
        y -= 13;
        page.AddText("- Herramientas y Metodologias: Git, GitHub, Docker basico, Postman, Scrum, Clean Code.", 9, new PdfPoint(60, y), regularFont);

        // Idiomas y Certificaciones
        y -= 22;
        page.AddText("IDIOMAS Y CERTIFICACIONES", 11, new PdfPoint(50, y), boldFont);
        y -= 15;
        page.AddText("- Idiomas: Espanol (Nativo)  |  Ingles: Nivel B2 Intermedio Alto (lectura fluida de documentacion tecnica).", 9, new PdfPoint(60, y), regularFont);
        y -= 13;
        page.AddText("- Certificacion: Microsoft Certified: Foundations of C# Programming (Microsoft Learn - 2024).", 9, new PdfPoint(60, y), regularFont);
        y -= 13;
        page.AddText("- Curso: Relational Database Design with PostgreSQL & SQL (Udemy - 2025).", 9, new PdfPoint(60, y), regularFont);

        // Generar archivo PDF
        byte[] pdfBytes = builder.Build();

        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string outputPath = Path.Combine(projectRoot, "CV_Mateo_Silva_Sin_Experiencia.pdf");
        File.WriteAllBytes(outputPath, pdfBytes);

        Assert.True(File.Exists(outputPath));
        Assert.True(pdfBytes.Length > 1000);
    }
}

