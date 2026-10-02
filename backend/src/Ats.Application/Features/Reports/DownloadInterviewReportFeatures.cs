using Ats.Application.Common.Interfaces;
using Ats.Domain.Common;

namespace Ats.Application.Features.Reports;

public record DownloadInterviewReportQuery(Guid CandidateId);
public record ReportDownload(Stream Content, string FileName);

public sealed class DownloadInterviewReportQueryHandler(
    IInterviewReportRepository reportRepository,
    IDocumentStorageService storageService,
    IReportDocumentRenderer documentRenderer,
    GetInterviewReportQueryHandler getReportHandler)
{
    public async Task<Result<ReportDownload>> HandleAsync(DownloadInterviewReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var reportResult = await getReportHandler.HandleAsync(new GetInterviewReportQuery(query.CandidateId), cancellationToken);
        if (reportResult.IsFailure) return Result.Failure<ReportDownload>(reportResult.Error);

        var report = await reportRepository.GetByCandidateIdAsync(query.CandidateId, cancellationToken);
        var fileName = $"informe_preentrevista_{query.CandidateId}.pdf";
        if (!string.IsNullOrWhiteSpace(report?.FileUrl))
        {
            var storedFile = await storageService.GetFileAsync(report.FileUrl, cancellationToken);
            if (storedFile is not null) return Result.Success(new ReportDownload(storedFile, fileName));
        }

        var rendered = await documentRenderer.RenderReportPdfAsync(reportResult.Value, cancellationToken);
        if (rendered.IsFailure) return Result.Failure<ReportDownload>(rendered.Error);
        if (rendered.Value.Length == 0)
            return Result.Failure<ReportDownload>(Error.Failure("Report.EmptyPdf", "El PDF generado está vacío."));
        return Result.Success(new ReportDownload(new MemoryStream(rendered.Value), fileName));
    }
}
