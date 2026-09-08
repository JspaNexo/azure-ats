using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Application.Features.CvProcessing;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.Enums;
using Ats.Domain.ValueObjects;
using Xunit;

namespace Ats.Application.UnitTests;

public class ProcessCvAnalysisCommandHandlerTests
{
    private readonly ICandidateRepository _candidateRepository = Substitute.For<ICandidateRepository>();
    private readonly ICvAnalysisRepository _cvAnalysisRepository = Substitute.For<ICvAnalysisRepository>();
    private readonly IProcessingJobRepository _processingJobRepository = Substitute.For<IProcessingJobRepository>();
    private readonly IDocumentStorageService _storageService = Substitute.For<IDocumentStorageService>();
    private readonly IPdfTextExtractor _pdfTextExtractor = Substitute.For<IPdfTextExtractor>();
    private readonly ICvAnalyzer _cvAnalyzer = Substitute.For<ICvAnalyzer>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ProcessCvAnalysisCommandHandler CreateHandler()
    {
        return new ProcessCvAnalysisCommandHandler(
            _candidateRepository,
            _cvAnalysisRepository,
            _processingJobRepository,
            _storageService,
            _pdfTextExtractor,
            _cvAnalyzer,
            _unitOfWork);
    }

    [Fact]
    public async Task ProcessCvAnalysis_WhenCandidateNotFound_ShouldReturnFailure()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new ProcessCvAnalysisCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _candidateRepository.GetByIdAsync(command.CandidateId, Arg.Any<CancellationToken>())
            .Returns((Candidate?)null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Candidate.NotFound");
    }

    [Fact]
    public async Task ProcessCvAnalysis_WhenAlreadyProcessed_ShouldReturnCachedAnalysisWithoutCallingAi()
    {
        // Arrange
        var handler = CreateHandler();
        var candidateId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var command = new ProcessCvAnalysisCommand(candidateId, documentId, Guid.NewGuid(), Guid.NewGuid());

        var cachedDto = new CvAnalysisDto(
            ProfessionalSummary: "Desarrollador Senior",
            CurrentRole: "Lead Architect",
            EstimatedSeniority: "Senior",
            TotalExperienceYears: 8,
            Skills: [new SkillDto(".NET", ".NET", "Backend", 8, "Exp", 0.9)],
            Languages: [],
            Education: [],
            Certifications: [],
            WorkExperience: [],
            PointsToValidate: [],
            Warnings: []);

        var existingCvAnalysis = CvAnalysis.CreatePending(candidateId, documentId, "Gemini", "flash-lite", "v1");
        existingCvAnalysis.MarkAsProcessed(JsonSerializer.Serialize(cachedDto));

        _cvAnalysisRepository.GetByCandidateIdAsync(candidateId, Arg.Any<CancellationToken>())
            .Returns(existingCvAnalysis);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentRole.Should().Be("Lead Architect");
        await _cvAnalyzer.DidNotReceive().AnalyzeCvTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
