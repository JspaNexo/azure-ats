using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Application.Features.Scoring;
using Ats.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ats.Application.UnitTests;

public class JobFitScoringServiceTests
{
    private readonly ISkillNormalizationService _skillNormalizer;
    private readonly JobFitScoringService _sut;

    public JobFitScoringServiceTests()
    {
        _skillNormalizer = Substitute.For<ISkillNormalizationService>();
        _skillNormalizer.Normalize(Arg.Any<string>()).Returns(ci =>
        {
            var raw = ci.Arg<string>();
            return raw switch
            {
                "c#" or "csharp" or ".net" => (".NET", "Backend"),
                "react" or "reactjs" => ("React", "Frontend"),
                "postgres" or "postgresql" => ("PostgreSQL", "Database"),
                "docker" => ("Docker", "DevOps"),
                _ => (raw, "General")
            };
        });

        _sut = new JobFitScoringService(_skillNormalizer);
    }

    [Fact]
    public void CalculateFit_WithoutJobPosition_ShouldReturnGenericFitScore()
    {
        // Arrange
        var cvAnalysis = new CvAnalysisDto(
            ProfessionalSummary: "Desarrollador backend experimentado",
            CurrentRole: "Backend Dev",
            EstimatedSeniority: "Senior",
            TotalExperienceYears: 6.0,
            Skills:
            [
                new SkillDto(Name: ".NET", Category: "Backend", ExperienceYears: 5.0, Confidence: 0.9, Evidence: "5 anos de backend"),
                new SkillDto(Name: "PostgreSQL", Category: "Database", ExperienceYears: 4.0, Confidence: 0.85, Evidence: "Diseno relacional")
            ]
        );

        // Act
        var result = _sut.CalculateFit(cvAnalysis, null);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeGreaterThan(50);
        result.Matches.Should().HaveCount(2);
        result.MissingRequired.Should().BeEmpty();
    }

    [Fact]
    public void CalculateFit_WithMatchingJobPosition_ShouldReturnHighFitScoreAndMatchedSkills()
    {
        // Arrange
        var jobPosition = JobPosition.Create(
            title: "Senior Backend Developer",
            description: "Buscamos dev backend",
            requirements: ".NET, PostgreSQL, Docker",
            department: "Engineering",
            seniority: "Senior",
            minExperienceYears: 4
        ).Value;

        var cvAnalysis = new CvAnalysisDto(
            ProfessionalSummary: "Dev .NET con experiencia en cloud y bases de datos",
            CurrentRole: "Senior Backend Engineer",
            EstimatedSeniority: "Senior",
            TotalExperienceYears: 5.0,
            Skills:
            [
                new SkillDto(Name: ".NET", Category: "Backend", ExperienceYears: 5.0, Confidence: 0.95, Evidence: "Proyectos empresariales"),
                new SkillDto(Name: "PostgreSQL", Category: "Database", ExperienceYears: 4.0, Confidence: 0.9, Evidence: "Optimizacion de queries"),
                new SkillDto(Name: "Docker", Category: "DevOps", ExperienceYears: 3.0, Confidence: 0.8, Evidence: "Contenedores en produccion")
            ]
        );

        // Act
        var result = _sut.CalculateFit(cvAnalysis, jobPosition);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeGreaterThanOrEqualTo(85);
        result.Matches.Should().HaveCount(3);
        result.Matches.All(m => m.IsMatched).Should().BeTrue();
        result.MissingRequired.Should().BeEmpty();
    }

    [Fact]
    public void CalculateFit_WithMissingSkillsAndLowExperience_ShouldReflectGapsInScore()
    {
        // Arrange
        var jobPosition = JobPosition.Create(
            title: "Tech Lead",
            description: "Lider tecnico",
            requirements: ".NET, React, Docker, Kubernetes",
            department: "Engineering",
            seniority: "Lead",
            minExperienceYears: 8
        ).Value;

        var cvAnalysis = new CvAnalysisDto(
            ProfessionalSummary: "Desarrollador junior iniciando carrera",
            CurrentRole: "Junior Developer",
            EstimatedSeniority: "Junior",
            TotalExperienceYears: 1.0,
            Skills:
            [
                new SkillDto(Name: "Python", Category: "Backend", ExperienceYears: 1.0, Confidence: 0.7, Evidence: "Scripting basico")
            ]
        );

        // Act
        var result = _sut.CalculateFit(cvAnalysis, jobPosition);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeLessThan(40);
        result.MissingRequired.Should().Contain(".NET");
        result.MissingRequired.Should().Contain("Docker");
    }
}
