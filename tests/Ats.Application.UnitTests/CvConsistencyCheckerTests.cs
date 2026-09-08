using Ats.Application.DTOs;
using Ats.Application.Features.CvProcessing;
using FluentAssertions;
using Xunit;

namespace Ats.Application.UnitTests;

public class CvConsistencyCheckerTests
{
    [Fact]
    public void CheckConsistency_WithConsistentCv_ShouldReturnEmptyWarnings()
    {
        // Arrange
        var cv = new CvAnalysisDto(
            ProfessionalSummary: "Ingeniera de software",
            CurrentRole: "Backend Developer",
            EstimatedSeniority: "Mid-level",
            TotalExperienceYears: 4.0,
            Skills:
            [
                new SkillDto(Name: ".NET", Category: "Backend", ExperienceYears: 3.0, Confidence: 0.9, Evidence: "3 anos"),
                new SkillDto(Name: "SQL Server", Category: "Database", ExperienceYears: 3.0, Confidence: 0.8, Evidence: "3 anos")
            ],
            WorkExperience:
            [
                new WorkExperienceDto(Role: "Backend Developer", Company: "Dev Co", DurationYears: 2.0),
                new WorkExperienceDto(Role: "Junior Developer", Company: "StartUp SA", DurationYears: 2.0)
            ],
            Education:
            [
                new EducationDto(Degree: "Ingenieria en Sistemas", Institution: "Universidad Nacional", GraduationYear: 2020)
            ],
            Certifications:
            [
                new CertificationDto(Name: "Azure Fundamentals", Issuer: "Microsoft", Year: 2021)
            ]
        );

        // Act
        var warnings = CvConsistencyChecker.CheckConsistency(cv);

        // Assert
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void CheckConsistency_WithExaggeratedSkillYears_ShouldReturnInconsistencyWarning()
    {
        // Arrange: candidato con 2 anos de trayectoria pero skill con 10 anos
        var cv = new CvAnalysisDto(
            ProfessionalSummary: "Desarrollador junior",
            CurrentRole: "Junior Dev",
            EstimatedSeniority: "Junior",
            TotalExperienceYears: 2.0,
            Skills:
            [
                new SkillDto(Name: "Kubernetes", Category: "DevOps", ExperienceYears: 10.0, Confidence: 0.9, Evidence: "10 anos")
            ]
        );

        // Act
        var warnings = CvConsistencyChecker.CheckConsistency(cv);

        // Assert
        warnings.Should().NotBeEmpty();
        warnings.Should().Contain(w => w.Contains("Kubernetes") && w.Contains("superando el total"));
    }

    [Fact]
    public void CheckConsistency_WithInflatedSkillCountForJunior_ShouldReturnProfileWarning()
    {
        // Arrange: candidato con 1 ano de exp y 14 skills listadas
        var skills = Enumerable.Range(1, 14)
            .Select(i => new SkillDto(Name: $"Skill_{i}", Category: "Tech", ExperienceYears: 1.0, Confidence: 0.7, Evidence: "Uso basico"))
            .ToList();

        var cv = new CvAnalysisDto(
            ProfessionalSummary: "Junior fullstack",
            CurrentRole: "Junior",
            EstimatedSeniority: "Junior",
            TotalExperienceYears: 1.0,
            Skills: skills
        );

        // Act
        var warnings = CvConsistencyChecker.CheckConsistency(cv);

        // Assert
        warnings.Should().NotBeEmpty();
        warnings.Should().Contain(w => w.Contains("posible perfil sobreestimado"));
    }

    [Fact]
    public void CheckConsistency_WithAnomalousGraduationYear_ShouldReturnEducationWarning()
    {
        // Arrange: fecha de graduacion imposible en el futuro lejano
        var cv = new CvAnalysisDto(
            ProfessionalSummary: "Especialista",
            CurrentRole: "Investigador",
            EstimatedSeniority: "Senior",
            TotalExperienceYears: 5.0,
            Education:
            [
                new EducationDto(Degree: "Doctorado", Institution: "Facultad del Futuro", GraduationYear: 2045)
            ]
        );

        // Act
        var warnings = CvConsistencyChecker.CheckConsistency(cv);

        // Assert
        warnings.Should().NotBeEmpty();
        warnings.Should().Contain(w => w.Contains("fuera del rango temporal esperado"));
    }
}
