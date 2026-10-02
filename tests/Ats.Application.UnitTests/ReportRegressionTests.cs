using System.Text.Json;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Application.Features.Reports;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ats.Application.UnitTests;

public class ReportRegressionTests
{
    private readonly Candidate _candidate = Candidate.Create("Ana", "Perez", CandidateEmail.Create("ana@example.com").Value).Value;
    private readonly ICandidateRepository _candidates = Substitute.For<ICandidateRepository>();
    private readonly ICvAnalysisRepository _analyses = Substitute.For<ICvAnalysisRepository>();
    private readonly IDiscRepository _disc = Substitute.For<IDiscRepository>();
    private readonly IInterviewReportRepository _reports = Substitute.For<IInterviewReportRepository>();
    private readonly IInterviewQuestionGenerator _questions = Substitute.For<IInterviewQuestionGenerator>();
    private readonly IReportDocumentRenderer _renderer = Substitute.For<IReportDocumentRenderer>();
    private readonly IDocumentStorageService _storage = Substitute.For<IDocumentStorageService>();
    private readonly CvAnalysis _analysis;
    private readonly DiscInterpretation _interpretation;

    public ReportRegressionTests()
    {
        _analysis = CvAnalysis.CreatePending(_candidate.Id, Guid.NewGuid(), "test", "test", "v1");
        _analysis.MarkAsProcessed(JsonSerializer.Serialize(new CvAnalysisDto()));
        _interpretation = DiscInterpretation.CreatePending(_candidate.Id, Guid.NewGuid(), "test", "test", "v1");
        _interpretation.MarkAsProcessed(JsonSerializer.Serialize(new DiscInterpretationDto()));
        _candidates.GetByIdAsync(_candidate.Id, Arg.Any<CancellationToken>()).Returns(_candidate);
        _analyses.GetByCandidateIdAsync(_candidate.Id, Arg.Any<CancellationToken>()).Returns(_analysis);
        _disc.GetInterpretationByCandidateIdAsync(_candidate.Id, Arg.Any<CancellationToken>()).Returns(_interpretation);
        _questions.GenerateQuestionsAsync(Arg.Any<CvAnalysisDto>(), Arg.Any<DiscInterpretationDto>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new InterviewQuestionsDto()));
        _renderer.RenderReportPdfAsync(Arg.Any<InterviewReportDto>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new byte[] { 1, 2, 3 }));
        _storage.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("new.pdf");
    }

    private GenerateInterviewReportCommandHandler CreateHandler() => new(_candidates, _analyses, _disc, _reports,
        Substitute.For<IProcessingJobRepository>(), _questions, _renderer, _storage, Substitute.For<IUnitOfWork>());

    private InterviewReport ExistingReport(Guid analysisId, Guid interpretationId)
    {
        var report = InterviewReport.CreateInitial(_candidate.Id, 4);
        var dto = new InterviewReportDto(new("Ana Perez", "Developer", 3, "Profile"),
            new([], [], [], [], []), new("D", "DISC summary", [], []), [], new([], [], []), "Disclaimer", "old.pdf");
        report.MarkAsGenerated(analysisId, interpretationId, JsonSerializer.Serialize(dto), "old.pdf", "test", "test", "v1");
        _reports.GetByCandidateIdAsync(_candidate.Id, Arg.Any<CancellationToken>()).Returns(report);
        return report;
    }

    [Fact]
    public async Task StaleReport_IsRegeneratedWithANewVersion()
    {
        ExistingReport(Guid.NewGuid(), _interpretation.Id);
        var result = await CreateHandler().HandleAsync(new GenerateInterviewReportCommand(_candidate.Id, Guid.NewGuid(), Guid.NewGuid()));
        result.Value.FileUrl.Should().Be("new.pdf");
        await _reports.Received(1).AddAsync(Arg.Is<InterviewReport>(r => r.Version == 5 && r.CvAnalysisId == _analysis.Id), Arg.Any<CancellationToken>());
        await _questions.Received(1).GenerateQuestionsAsync(Arg.Any<CvAnalysisDto>(), Arg.Any<DiscInterpretationDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnchangedSources_ReuseTheReportWithoutGeneratingQuestions()
    {
        ExistingReport(_analysis.Id, _interpretation.Id);
        var result = await CreateHandler().HandleAsync(new GenerateInterviewReportCommand(_candidate.Id, Guid.NewGuid(), Guid.NewGuid()));
        result.IsSuccess.Should().BeTrue();
        result.Value.FileUrl.Should().Be("old.pdf");
        result.Value.CandidateOverview.Name.Should().Be("Ana Perez");
        _questions.ReceivedCalls().Should().BeEmpty();
        await _reports.DidNotReceive().AddAsync(Arg.Any<InterviewReport>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailedPdf_DoesNotPersistASuccessfulReport()
    {
        _renderer.RenderReportPdfAsync(Arg.Any<InterviewReportDto>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<byte[]>(Error.Failure("Pdf.Failed", "Rendering failed")));
        var result = await CreateHandler().HandleAsync(new GenerateInterviewReportCommand(_candidate.Id, Guid.NewGuid(), Guid.NewGuid()));
        result.Error.Code.Should().Be("Pdf.Failed");
        await _reports.DidNotReceive().AddAsync(Arg.Any<InterviewReport>(), Arg.Any<CancellationToken>());
        _storage.ReceivedCalls().Should().BeEmpty();
    }
}
