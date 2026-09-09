using FluentAssertions;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.Enums;
using Ats.Domain.Events;
using Ats.Domain.ValueObjects;
using Xunit;

namespace Ats.Domain.UnitTests;

public class CandidateTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var emailResult = CandidateEmail.Create("juan.perez@example.com");
        emailResult.IsSuccess.Should().BeTrue();

        // Act
        var candidateResult = Candidate.Create("Juan", "Pérez", emailResult.Value, "+123456789");

        // Assert
        candidateResult.IsSuccess.Should().BeTrue();
        candidateResult.Value.FirstName.Should().Be("Juan");
        candidateResult.Value.LastName.Should().Be("Pérez");
        candidateResult.Value.Email.Value.Should().Be("juan.perez@example.com");
        candidateResult.Value.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyFirstName_ShouldReturnFailure(string? firstName)
    {
        // Arrange
        var email = CandidateEmail.Create("test@example.com").Value;

        // Act
        var result = Candidate.Create(firstName!, "Pérez", email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Candidate.FirstNameEmpty");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@missingusername.com")]
    [InlineData("missingdomain@")]
    public void CandidateEmail_WithInvalidFormat_ShouldReturnFailure(string invalidEmail)
    {
        // Act
        var result = CandidateEmail.Create(invalidEmail);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.InvalidFormat");
    }

    [Fact]
    public void AddCvDocument_ShouldAddDocumentAndRaiseDomainEvent()
    {
        // Arrange
        var email = CandidateEmail.Create("ana@example.com").Value;
        var candidate = Candidate.Create("Ana", "García", email).Value;

        // Act
        var doc = candidate.AddCvDocument("cv_ana.pdf", "storage/cv_ana.pdf", 1024, "application/pdf");

        // Assert
        candidate.Documents.Should().ContainSingle();
        doc.FileName.Should().Be("cv_ana.pdf");
        candidate.GetDomainEvents().Should().ContainSingle();
    }
}

public class DiscScoresTests
{
    [Fact]
    public void Create_WithValidScores_ShouldDeterminePrimaryStyle()
    {
        // Act (D=85, C=80, I=40, S=30 -> D/C)
        var result = DiscScores.Create(85, 40, 30, 80);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PrimaryStyle.Should().Be("D/C");
        result.Value.Dominance.Should().Be(85);
        result.Value.Conscientiousness.Should().Be(80);
    }

    [Fact]
    public void Create_WithOutOfRangeScores_ShouldReturnFailure()
    {
        // Act
        var result = DiscScores.Create(150, 40, 30, 80);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiscScores.OutOfRange");
    }
}

public class CvAnalysisTests
{
    [Fact]
    public void CvAnalysis_MarkAsProcessed_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var analysis = CvAnalysis.CreatePending(candidateId, documentId, "Gemini", "flash", "v1");

        // Act
        analysis.MarkAsProcessed("{\"summary\":\"Test\"}");

        // Assert
        analysis.Status.Should().Be(ProcessingStatus.Processed);
        analysis.AnalysisJson.Should().Be("{\"summary\":\"Test\"}");
        analysis.GetDomainEvents().Should().ContainSingle();
    }
}

public class AssessmentScoresTests
{
    [Fact]
    public void CreateDisc_WithValidScores_ShouldPopulateDimensionsAndStyle()
    {
        // Act (D=85, I=70, S=40, C=90)
        var result = AssessmentScores.CreateDisc(85, 70, 40, 90);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AssessmentType.Should().Be("DISC");
        result.Value.PrimaryStyle.Should().Be("C/D");
        result.Value.Dominance.Should().Be(85);
        result.Value.Influence.Should().Be(70);
        result.Value.Steadiness.Should().Be(40);
        result.Value.Conscientiousness.Should().Be(90);
        result.Value.Dimensions.Should().ContainKey("Dominance");
        result.Value.Dimensions["Dominance"].Should().Be(85);
    }

    [Fact]
    public void Create_WithCustomDimensions_ShouldSucceed()
    {
        // Arrange
        var dimensions = new Dictionary<string, double>
        {
            { "Openness", 85.5 },
            { "Conscientiousness", 92.0 },
            { "Extraversion", 65.0 },
            { "Agreeableness", 78.0 },
            { "Neuroticism", 20.0 }
        };

        // Act
        var result = AssessmentScores.Create("BigFive", dimensions, "Analítico/Estructurado");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AssessmentType.Should().Be("BigFive");
        result.Value.PrimaryStyle.Should().Be("Analítico/Estructurado");
        result.Value.Dimensions.Should().HaveCount(5);
        result.Value.Dimensions["Openness"].Should().Be(85.5);
    }

    [Fact]
    public void Create_WithOutOfRangeValue_ShouldReturnFailure()
    {
        // Arrange
        var dimensions = new Dictionary<string, double>
        {
            { "SkillScore", 120.0 }
        };

        // Act
        var result = AssessmentScores.Create("Technical", dimensions);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AssessmentScores.OutOfRange");
    }
}

public class CandidateAssessmentTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeAndRaiseDomainEvent()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var scores = AssessmentScores.Create("Cognitive", new Dictionary<string, double> { { "ProblemSolving", 90.0 } }, "Alto Rendimiento").Value;

        // Act
        var result = CandidateAssessment.Create(candidateId, scores, "{\"raw\":123}");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CandidateId.Should().Be(candidateId);
        result.Value.AssessmentType.Should().Be("Cognitive");
        result.Value.Scores.Dimensions["ProblemSolving"].Should().Be(90.0);
        result.Value.RawResultsJson.Should().Be("{\"raw\":123}");
        result.Value.GetDomainEvents().Should().ContainSingle();
        result.Value.GetDomainEvents().First().Should().BeOfType<AssessmentCompletedDomainEvent>();
    }

    [Fact]
    public void AssessmentInterpretation_CreatePending_AndMarkAsProcessed_ShouldUpdateStatus()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var interpretation = AssessmentInterpretation.CreatePending(candidateId, assessmentId, "BigFive", "Gemini", "flash", "v1");

        // Act
        interpretation.MarkAsProcessed("{\"summary\":\"Perfil equilibrado\"}");

        // Assert
        interpretation.Status.Should().Be(ProcessingStatus.Processed);
        interpretation.InterpretationJson.Should().Be("{\"summary\":\"Perfil equilibrado\"}");
        interpretation.GetDomainEvents().Should().ContainSingle();
        interpretation.GetDomainEvents().First().Should().BeOfType<AssessmentInterpretationCompletedDomainEvent>();
    }
}

