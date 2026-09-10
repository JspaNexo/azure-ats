using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ats.Application.DTOs;
using Ats.Application.Features.Reports;
using Ats.Domain.Common;

namespace Ats.Api.Controllers;

[Authorize]
[Route("api/v1/reports")]
public class ReportsController : ApiControllerBase
{
    private readonly GetInterviewReportQueryHandler _getQueryHandler;
    private readonly GenerateInterviewReportCommandHandler _generateHandler;

    public ReportsController(
        GetInterviewReportQueryHandler getQueryHandler,
        GenerateInterviewReportCommandHandler generateHandler)
    {
        _getQueryHandler = getQueryHandler;
        _generateHandler = generateHandler;
    }

    [HttpGet("candidate/{candidateId:guid}")]
    [ProducesResponseType(typeof(InterviewReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCandidateId([FromRoute] Guid candidateId, CancellationToken cancellationToken)
    {
        var query = new GetInterviewReportQuery(candidateId);
        var result = await _getQueryHandler.HandleAsync(query, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(InterviewReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateReport(
        [FromQuery] Guid candidateId,
        [FromQuery] Guid? eventId,
        [FromQuery] Guid? correlationId,
        CancellationToken cancellationToken)
    {
        var command = new GenerateInterviewReportCommand(
            candidateId,
            eventId ?? Guid.NewGuid(),
            correlationId ?? Guid.NewGuid());

        var result = await _generateHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("download/{candidateId:guid}")]
    public async Task<IActionResult> DownloadReportPdf(
        [FromRoute] Guid candidateId,
        [FromQuery] bool inline,
        [FromServices] GetReportPdfQueryHandler pdfQueryHandler,
        CancellationToken cancellationToken)
    {
        var query = new GetReportPdfQuery(candidateId);
        var result = await pdfQueryHandler.HandleAsync(query, cancellationToken);
        
        if (!result.IsSuccess)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(CreateProblemDetails(result.Error, StatusCodes.Status404NotFound)),
                ErrorType.Validation => BadRequest(CreateProblemDetails(result.Error, StatusCodes.Status400BadRequest)),
                _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result.Error, StatusCodes.Status500InternalServerError))
            };
        }

        if (inline)
        {
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"informe_preentrevista_{candidateId}.pdf\"");
            return File(result.Value, "application/pdf");
        }

        return File(result.Value, "application/pdf", $"informe_preentrevista_{candidateId}.pdf");
    }

    [HttpGet("view/{candidateId:guid}")]
    public async Task<IActionResult> ViewReportPdf(
        [FromRoute] Guid candidateId,
        [FromServices] GetReportPdfQueryHandler pdfQueryHandler,
        CancellationToken cancellationToken)
    {
        return await DownloadReportPdf(candidateId, inline: true, pdfQueryHandler, cancellationToken);
    }
}
