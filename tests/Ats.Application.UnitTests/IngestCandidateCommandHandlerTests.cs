using System.Text;
using FluentAssertions;
using NSubstitute;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Application.Features.Candidates;
using Ats.Application.Features.CvProcessing;
using Ats.Application.Features.Disc;
using Ats.Application.Features.Documents;
using Ats.Application.Features.Ingestion;
using Ats.Application.Features.Reports;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.ValueObjects;
using Xunit;

namespace Ats.Application.UnitTests;

public class IngestCandidateCommandHandlerTests
{
    private readonly ICandidateRepository _candidateRepository = Substitute.For<ICandidateRepository>();
    private readonly IJobPositionRepository _jobPositionRepository = Substitute.For<IJobPositionRepository>();
    private readonly IBackgroundJobQueue _backgroundJobQueue = Substitute.For<IBackgroundJobQueue>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDocumentStorageService _storageService = Substitute.For<IDocumentStorageService>();
    private readonly IDiscRepository _discRepository = Substitute.For<IDiscRepository>();
    private readonly ICvAnalysisRepository _cvAnalysisRepository = Substitute.For<ICvAnalysisRepository>();
    private readonly IInterviewReportRepository _reportRepository = Substitute.For<IInterviewReportRepository>();
    private readonly IProcessingJobRepository _processingJobRepository = Substitute.For<IProcessingJobRepository>();
    private readonly ICvAnalyzer _cvAnalyzer = Substitute.For<ICvAnalyzer>();
    private readonly IPdfTextExtractor _pdfTextExtractor = Substitute.For<IPdfTextExtractor>();
    private readonly IDiscInterpreter _discInterpreter = Substitute.For<IDiscInterpreter>();
    private readonly IInterviewQuestionGenerator _questionGenerator = Substitute.For<IInterviewQuestionGenerator>();
    private readonly IReportDocumentRenderer _documentRenderer = Substitute.For<IReportDocumentRenderer>();

    private IngestCandidateCommandHandler CreateHandler()
    {
        var registerHandler = new RegisterCandidateCommandHandler(_candidateRepository, _unitOfWork);
        var uploadCvHandler = new UploadCvCommandHandler(_candidateRepository, _storageService, _unitOfWork);
        var submitDiscHandler = new SubmitDiscResultCommandHandler(_candidateRepository, _discRepository, _unitOfWork);
        var processCvHandler = new ProcessCvAnalysisCommandHandler(
            _candidateRepository,
            _cvAnalysisRepository,
            _processingJobRepository,
            _storageService,
            _pdfTextExtractor,
            _cvAnalyzer,
            _unitOfWork);
        var processDiscHandler = new ProcessDiscInterpretationCommandHandler(
            _discRepository,
            _processingJobRepository,
            _discInterpreter,
            _unitOfWork);
        var generateReportHandler = new GenerateInterviewReportCommandHandler(
            _candidateRepository,
            _cvAnalysisRepository,
            _discRepository,
            _reportRepository,
            _processingJobRepository,
            _questionGenerator,
            _documentRenderer,
            _storageService,
            _unitOfWork);
        var getCandidateByIdHandler = new GetCandidateByIdQueryHandler(
            _candidateRepository,
            _cvAnalysisRepository,
            _discRepository,
            _reportRepository,
            _jobPositionRepository);

        return new IngestCandidateCommandHandler(
            registerHandler,
            uploadCvHandler,
            submitDiscHandler,
            processCvHandler,
            processDiscHandler,
            generateReportHandler,
            getCandidateByIdHandler,
            _candidateRepository,
            _jobPositionRepository,
            _backgroundJobQueue);
    }

    [Fact]
    public async Task IngestCandidate_WithAsyncTrue_ShouldEnqueueBackgroundJobAndReturnCandidate()
    {
        // Arrange
        var handler = CreateHandler();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 dummy content"));

        _candidateRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Candidate?)null);

        var email = CandidateEmail.Create("ana.gomez@example.com").Value;
        var createdCandidate = Candidate.Create("Ana", "Gomez", email).Value;
        _candidateRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(createdCandidate);

        _storageService.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("storage/unique_test.pdf");

        var command = new IngestCandidateCommand(
            FirstName: "Ana",
            LastName: "Gomez",
            Email: "ana.gomez@example.com",
            PhoneNumber: "+541122334455",
            CvStream: stream,
            FileName: "cv_ana.pdf",
            FileSizeBytes: 1024,
            ContentType: "application/pdf",
            Dominance: 70,
            Influence: 60,
            Steadiness: 50,
            Conscientiousness: 80,
            PrimaryStyle: "D/C",
            TargetRole: "Fullstack Developer",
            Asynchronous: true);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Ana");
        result.Value.LastName.Should().Be("Gomez");
        _backgroundJobQueue.Received(1).Enqueue(Arg.Any<Func<IServiceProvider, CancellationToken, Task>>());
    }
}
