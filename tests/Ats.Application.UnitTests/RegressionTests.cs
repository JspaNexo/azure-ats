using System.Text;
using System.Text.Json;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Application.Features.Candidates;
using Ats.Application.Features.CvProcessing;
using Ats.Application.Features.Disc;
using Ats.Application.Features.Documents;
using Ats.Application.Features.JobPositions;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ats.Application.UnitTests;

public class RegressionTests
{
    private readonly ICandidateRepository _candidates = Substitute.For<ICandidateRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private static Candidate CreateCandidate() => Candidate.Create("Ana", "Perez", CandidateEmail.Create("ana@example.com").Value).Value;

    [Fact]
    public async Task InvalidRegistration_DoesNotAccessPersistence()
    {
        var handler = new RegisterCandidateCommandHandler(_candidates, _unitOfWork, new RegisterCandidateCommandValidator());
        var result = await handler.HandleAsync(new RegisterCandidateCommand(new string('a', 101), "Perez", "ana@example.com", null));
        result.Error.Type.Should().Be(ErrorType.Validation);
        _candidates.ReceivedCalls().Should().BeEmpty();
        _unitOfWork.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Registration_NormalizesEmailBeforeDuplicateLookup()
    {
        var candidate = CreateCandidate();
        _candidates.GetByEmailAsync("ana@example.com", Arg.Any<CancellationToken>()).Returns(candidate);
        var handler = new RegisterCandidateCommandHandler(_candidates, _unitOfWork, new RegisterCandidateCommandValidator());
        var result = await handler.HandleAsync(new RegisterCandidateCommand("Ana", "Perez", "ANA@example.com", null));
        result.Error.Code.Should().Be("Candidate.EmailExists");
        await _candidates.DidNotReceive().AddAsync(Arg.Any<Candidate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidPdf_IsRejectedBeforeStorageOrDatabaseWrites()
    {
        var storage = Substitute.For<IDocumentStorageService>();
        var handler = new UploadCvCommandHandler(_candidates, storage, _unitOfWork, new UploadCvCommandValidator());
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not a PDF"));
        var result = await handler.HandleAsync(new UploadCvCommand(Guid.NewGuid(), stream, "cv.pdf", stream.Length, "application/pdf"));
        result.Error.Code.Should().Be("File.InvalidFormat");
        stream.Position.Should().Be(0);
        storage.ReceivedCalls().Should().BeEmpty();
        _candidates.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(15 * 1024 * 1024, true)]
    [InlineData(15 * 1024 * 1024 + 1, false)]
    public void UploadSizeLimit_MatchesTheIngestionLimit(long bytes, bool expected)
    {
        using var stream = new MemoryStream();
        var command = new UploadCvCommand(Guid.NewGuid(), stream, "cv.pdf", bytes, "application/pdf");
        new UploadCvCommandValidator().Validate(command).IsValid.Should().Be(expected);
    }

    [Fact]
    public async Task CandidateWithoutAnalysis_DoesNotExposeInventedMetrics()
    {
        var candidate = CreateCandidate();
        var repository = Substitute.For<ICandidateDetailsRepository>();
        repository.GetByIdAsync(candidate.Id, Arg.Any<CancellationToken>()).Returns(new CandidateDetails(candidate, null, null, null, null));
        var result = await new GetCandidateByIdQueryHandler(repository).HandleAsync(new GetCandidateByIdQuery(candidate.Id));
        result.Value.Status.Should().Be("Registered");
        result.Value.MatchScore.Should().BeNull();
        result.Value.ExperienceYears.Should().BeNull();
        result.Value.DiscScores.Should().BeNull();
        result.Value.Seniority.Should().Be("Sin evaluar");
    }

    [Fact]
    public async Task CandidateDetail_UsesPersistedDiscScores()
    {
        var candidate = CreateCandidate();
        var disc = DiscResult.Create(candidate.Id, DiscScores.Create(10, 20, 30, 90).Value).Value;
        var repository = Substitute.For<ICandidateDetailsRepository>();
        repository.GetByIdAsync(candidate.Id, Arg.Any<CancellationToken>()).Returns(new CandidateDetails(candidate, null, disc, null, null));
        var result = await new GetCandidateByIdQueryHandler(repository).HandleAsync(new GetCandidateByIdQuery(candidate.Id));
        result.Value.DiscScores.Should().Be(new DiscScoresDto(10, 20, 30, 90));
        result.Value.PrimaryDiscStyle.Should().Be("C");
    }

    [Fact]
    public async Task NewCv_DoesNotReuseThePreviousDocumentAnalysis()
    {
        var candidate = CreateCandidate();
        var document = candidate.AddCvDocument("new.pdf", "new.pdf", 123, "application/pdf");
        var analyses = Substitute.For<ICvAnalysisRepository>();
        var previous = CvAnalysis.CreatePending(candidate.Id, Guid.NewGuid(), "test", "test", "v1");
        previous.MarkAsProcessed(JsonSerializer.Serialize(new CvAnalysisDto(ProfessionalSummary: "Old")));
        analyses.GetByCandidateIdAsync(candidate.Id, Arg.Any<CancellationToken>()).Returns(previous);
        _candidates.GetByIdAsync(candidate.Id, Arg.Any<CancellationToken>()).Returns(candidate);
        var storage = Substitute.For<IDocumentStorageService>();
        storage.GetFileAsync(document.StoragePath, Arg.Any<CancellationToken>()).Returns(new MemoryStream());
        var extractor = Substitute.For<IPdfTextExtractor>();
        extractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(new string('x', 100));
        var analyzer = Substitute.For<ICvAnalyzer>();
        analyzer.AnalyzeCvTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new CvAnalysisDto(ProfessionalSummary: "New")));
        var handler = new ProcessCvAnalysisCommandHandler(_candidates, analyses, Substitute.For<IProcessingJobRepository>(), storage, extractor, analyzer, _unitOfWork);
        var result = await handler.HandleAsync(new ProcessCvAnalysisCommand(candidate.Id, document.Id, Guid.NewGuid(), Guid.NewGuid()));
        result.Value.ProfessionalSummary.Should().Be("New");
        await analyses.Received(1).AddAsync(Arg.Is<CvAnalysis>(a => a.DocumentId == document.Id), Arg.Any<CancellationToken>());
        previous.AnalysisJson.Should().Contain("Old");
    }

    [Fact]
    public async Task NewDiscResult_GeneratesANewInterpretation()
    {
        var candidate = CreateCandidate();
        var results = Substitute.For<IDiscRepository>();
        var disc = DiscResult.Create(candidate.Id, DiscScores.Create(12, 34, 56, 78).Value).Value;
        var previous = DiscInterpretation.CreatePending(candidate.Id, Guid.NewGuid(), "test", "test", "v1");
        previous.MarkAsProcessed(JsonSerializer.Serialize(new DiscInterpretationDto(Summary: "Old")));
        results.GetInterpretationByCandidateIdAsync(candidate.Id, Arg.Any<CancellationToken>()).Returns(previous);
        results.GetResultByIdAsync(disc.Id, Arg.Any<CancellationToken>()).Returns(disc);
        var interpreter = Substitute.For<IDiscInterpreter>();
        interpreter.InterpretDiscAsync(disc.Scores, Arg.Any<CancellationToken>()).Returns(Result.Success(new DiscInterpretationDto(Summary: "New")));
        var handler = new ProcessDiscInterpretationCommandHandler(results, Substitute.For<IProcessingJobRepository>(), interpreter, _unitOfWork);
        var result = await handler.HandleAsync(new ProcessDiscInterpretationCommand(candidate.Id, disc.Id, Guid.NewGuid(), Guid.NewGuid()));
        result.Value.Summary.Should().Be("New");
        await results.Received(1).AddInterpretationAsync(Arg.Is<DiscInterpretation>(i => i.DiscResultId == disc.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DiscResultForAnotherCandidate_IsRejectedBeforeJobCreation()
    {
        var repository = Substitute.For<IDiscRepository>();
        var disc = DiscResult.Create(Guid.NewGuid(), DiscScores.Create(50, 50, 50, 50).Value).Value;
        repository.GetResultByIdAsync(disc.Id, Arg.Any<CancellationToken>()).Returns(disc);
        var jobs = Substitute.For<IProcessingJobRepository>();
        var handler = new ProcessDiscInterpretationCommandHandler(repository, jobs, Substitute.For<IDiscInterpreter>(), _unitOfWork);
        var result = await handler.HandleAsync(new ProcessDiscInterpretationCommand(Guid.NewGuid(), disc.Id, Guid.NewGuid(), Guid.NewGuid()));
        result.Error.Code.Should().Be("Disc.NotFound");
        await jobs.DidNotReceive().AddAsync(Arg.Any<ProcessingJob>(), Arg.Any<CancellationToken>());
        _unitOfWork.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task NegativeVacancyExperience_IsRejectedBeforePersistence()
    {
        var repository = Substitute.For<IJobPositionRepository>();
        var handler = new CreateJobPositionCommandHandler(repository, _unitOfWork, new CreateJobPositionCommandValidator());
        var result = await handler.HandleAsync(new CreateJobPositionCommand("Developer", "IT", "Junior", -1, null, null));
        result.Error.Type.Should().Be(ErrorType.Validation);
        repository.ReceivedCalls().Should().BeEmpty();
    }
}
