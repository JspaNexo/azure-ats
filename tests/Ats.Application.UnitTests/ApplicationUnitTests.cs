using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Application.Features.Assessments;
using Ats.Application.Features.Candidates;
using Ats.Application.Features.Disc;
using Ats.Application.Features.Reports;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.ValueObjects;
using Xunit;

namespace Ats.Application.UnitTests;

public class ApplicationUnitTests
{
    private readonly ICandidateRepository _candidateRepository = Substitute.For<ICandidateRepository>();
    private readonly IDiscRepository _discRepository = Substitute.For<IDiscRepository>();
    private readonly ICandidateAssessmentRepository _candidateAssessmentRepository = Substitute.For<ICandidateAssessmentRepository>();
    private readonly ICvAnalysisRepository _cvAnalysisRepository = Substitute.For<ICvAnalysisRepository>();
    private readonly IInterviewReportRepository _reportRepository = Substitute.For<IInterviewReportRepository>();
    private readonly IProcessingJobRepository _processingJobRepository = Substitute.For<IProcessingJobRepository>();
    private readonly IInterviewQuestionGenerator _questionGenerator = Substitute.For<IInterviewQuestionGenerator>();
    private readonly IReportDocumentRenderer _documentRenderer = Substitute.For<IReportDocumentRenderer>();
    private readonly IDocumentStorageService _storageService = Substitute.For<IDocumentStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task RegisterCandidate_WithNewEmail_ShouldReturnSuccess()
    {
        // Arrange
        _candidateRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Candidate?)null);

        var handler = new RegisterCandidateCommandHandler(_candidateRepository, _unitOfWork);
        var command = new RegisterCandidateCommand("Carlos", "Santana", "carlos@example.com", "+5491112345678");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Carlos");
        result.Value.Email.Should().Be("carlos@example.com");
        await _candidateRepository.Received(1).AddAsync(Arg.Any<Candidate>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterCandidate_WithExistingEmail_ShouldReturnConflict()
    {
        // Arrange
        var email = CandidateEmail.Create("existing@example.com").Value;
        var existingCandidate = Candidate.Create("Existing", "User", email).Value;

        _candidateRepository.GetByEmailAsync("existing@example.com", Arg.Any<CancellationToken>())
            .Returns(existingCandidate);

        var handler = new RegisterCandidateCommandHandler(_candidateRepository, _unitOfWork);
        var command = new RegisterCandidateCommand("Carlos", "Santana", "existing@example.com", null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Candidate.EmailExists");
    }

    [Fact]
    public async Task SubmitDiscResult_WithValidScores_ShouldPersistResult()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var email = CandidateEmail.Create("test@example.com").Value;
        var candidate = Candidate.Create("Test", "User", email).Value;

        _candidateRepository.GetByIdAsync(candidateId, Arg.Any<CancellationToken>())
            .Returns(candidate);

        var handler = new SubmitDiscResultCommandHandler(_candidateRepository, _discRepository, _unitOfWork);
        var command = new SubmitDiscResultCommand(candidateId, 80, 50, 40, 75, null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _discRepository.Received(1).AddResultAsync(Arg.Any<DiscResult>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateInterviewReport_WhenCvAndDiscAreReady_ShouldGenerateTwoPageReport()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var email = CandidateEmail.Create("candidato@example.com").Value;
        var candidate = Candidate.Create("Laura", "Gómez", email).Value;

        var cvAnalysis = CvAnalysis.CreatePending(candidateId, Guid.NewGuid(), "Gemini", "flash", "v1");
        var cvDto = new CvAnalysisDto(
            ProfessionalSummary: "Experta en .NET y Cloud",
            CurrentRole: "Senior Backend Developer",
            EstimatedSeniority: "Senior",
            TotalExperienceYears: 6,
            Skills: [new SkillDto(".NET", ".NET", "Backend", 6, "6 años de experiencia", 0.95)],
            Languages: [new LanguageDto("Inglés", "C1", "Fluido")],
            Education: [],
            Certifications: [],
            WorkExperience: [],
            PointsToValidate: ["Liderazgo técnico"],
            Warnings: []
        );
        cvAnalysis.MarkAsProcessed(JsonSerializer.Serialize(cvDto));

        var discInterpretation = DiscInterpretation.CreatePending(candidateId, Guid.NewGuid(), "Gemini", "flash", "v1");
        var discDto = new DiscInterpretationDto(
            PrimaryStyle: "D",
            Summary: "Orientada a resultados rápidos.",
            StrengthsToExplore: ["Liderazgo"],
            PointsToExplore: ["Paciencia en procesos repetitivos"],
            BehavioralQuestionTopics: ["Toma de decisiones"],
            Disclaimer: "Aviso de uso responsable"
        );
        discInterpretation.MarkAsProcessed(JsonSerializer.Serialize(discDto));

        _candidateRepository.GetByIdAsync(candidateId, Arg.Any<CancellationToken>()).Returns(candidate);
        _cvAnalysisRepository.GetByCandidateIdAsync(candidateId, Arg.Any<CancellationToken>()).Returns(cvAnalysis);
        _discRepository.GetInterpretationByCandidateIdAsync(candidateId, Arg.Any<CancellationToken>()).Returns(discInterpretation);

        _questionGenerator.GenerateQuestionsAsync(Arg.Any<CvAnalysisDto>(), Arg.Any<DiscInterpretationDto>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new InterviewQuestionsDto(
                ProfessionalQuestions: ["Pregunta 1"],
                TechnicalQuestions: ["Pregunta 2"],
                BehavioralQuestions: ["Pregunta 3"])));

        _documentRenderer.RenderReportPdfAsync(Arg.Any<InterviewReportDto>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new byte[] { 1, 2, 3 }));

        _storageService.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("storage/report.pdf");

        var handler = new GenerateInterviewReportCommandHandler(
            _candidateRepository,
            _cvAnalysisRepository,
            _discRepository,
            _reportRepository,
            _processingJobRepository,
            _questionGenerator,
            _documentRenderer,
            _storageService,
            _unitOfWork);

        var command = new GenerateInterviewReportCommand(candidateId, Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CandidateOverview.Name.Should().Be("Laura Gómez");
        result.Value.InterviewGuide.ProfessionalQuestions.Should().ContainSingle();
        result.Value.InterviewGuide.TechnicalQuestions.Should().ContainSingle();
        result.Value.InterviewGuide.BehavioralQuestions.Should().ContainSingle();
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAssessmentResult_WithNonDiscType_ShouldPersistGenericAssessmentAndNotLegacyDisc()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var email = CandidateEmail.Create("maria@example.com").Value;
        var candidate = Candidate.Create("María", "López", email).Value;

        _candidateRepository.GetByIdAsync(candidateId, Arg.Any<CancellationToken>())
            .Returns(candidate);

        var handler = new SubmitAssessmentResultCommandHandler(
            _candidateRepository,
            _candidateAssessmentRepository,
            _discRepository,
            _unitOfWork);

        var dimensions = new Dictionary<string, double>
        {
            { "Openness", 85.0 },
            { "Conscientiousness", 90.0 }
        };

        var command = new SubmitAssessmentResultCommand(candidateId, "BigFive", dimensions, "Estructurado");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _candidateAssessmentRepository.Received(1).AddResultAsync(Arg.Any<CandidateAssessment>(), Arg.Any<CancellationToken>());
        await _discRepository.DidNotReceive().AddResultAsync(Arg.Any<DiscResult>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCandidateAssessment_WhenAssessmentExists_ShouldReturnDetailDto()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var scores = AssessmentScores.Create("BigFive", new Dictionary<string, double> { { "Openness", 80.0 } }, "Creativo").Value;
        var assessment = CandidateAssessment.Create(candidateId, scores).Value;

        _candidateAssessmentRepository.GetResultByCandidateIdAsync(candidateId, Arg.Any<CancellationToken>())
            .Returns(assessment);
        _candidateAssessmentRepository.GetInterpretationByCandidateIdAsync(candidateId, Arg.Any<CancellationToken>())
            .Returns((AssessmentInterpretation?)null);

        var handler = new GetCandidateAssessmentQueryHandler(_candidateAssessmentRepository);
        var query = new GetCandidateAssessmentQuery(candidateId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AssessmentType.Should().Be("BigFive");
        result.Value.Scores.PrimaryStyle.Should().Be("Creativo");
        result.Value.Scores.Dimensions["Openness"].Should().Be(80.0);
    }
}

