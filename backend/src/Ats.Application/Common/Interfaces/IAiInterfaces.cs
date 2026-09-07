using Ats.Application.DTOs;
using Ats.Domain.Common;
using Ats.Domain.ValueObjects;

namespace Ats.Application.Common.Interfaces;

public interface ICvAnalyzer
{
    Task<Result<CvAnalysisDto>> AnalyzeCvTextAsync(string cvText, CancellationToken cancellationToken = default);
}

public interface IDiscInterpreter
{
    Task<Result<DiscInterpretationDto>> InterpretDiscAsync(DiscScores scores, CancellationToken cancellationToken = default);
}

public interface IInterviewQuestionGenerator
{
    Task<Result<InterviewQuestionsDto>> GenerateQuestionsAsync(
        CvAnalysisDto cvAnalysis,
        DiscInterpretationDto discInterpretation,
        CancellationToken cancellationToken = default);
}

public interface IReportDocumentRenderer
{
    Task<Result<byte[]>> RenderReportPdfAsync(InterviewReportDto reportDto, CancellationToken cancellationToken = default);
}

