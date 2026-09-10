using Microsoft.AspNetCore.Mvc;
using Ats.Application.Features.CvProcessing;
using Ats.Application.Features.Disc;
using Ats.Application.Features.Reports;

namespace Ats.Api.Controllers;

[Route("api/v1/webhooks")]
public class WebhooksController : ApiControllerBase
{
    private readonly ProcessCvAnalysisCommandHandler _cvAnalysisHandler;
    private readonly ProcessDiscInterpretationCommandHandler _discInterpretationHandler;
    private readonly GenerateInterviewReportCommandHandler _reportGenerationHandler;
    private readonly IConfiguration _configuration;

    public WebhooksController(
        ProcessCvAnalysisCommandHandler cvAnalysisHandler,
        ProcessDiscInterpretationCommandHandler discInterpretationHandler,
        GenerateInterviewReportCommandHandler reportGenerationHandler,
        IConfiguration configuration)
    {
        _cvAnalysisHandler = cvAnalysisHandler;
        _discInterpretationHandler = discInterpretationHandler;
        _reportGenerationHandler = reportGenerationHandler;
        _configuration = configuration;
    }

    [HttpPost("process-cv")]
    public async Task<IActionResult> ProcessCv(
        [FromQuery] Guid candidateId,
        [FromQuery] Guid documentId,
        [FromQuery] Guid eventId,
        [FromQuery] Guid correlationId,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return UnauthorizedResponse();
        }

        var command = new ProcessCvAnalysisCommand(candidateId, documentId, eventId, correlationId);
        var result = await _cvAnalysisHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("process-disc")]
    public async Task<IActionResult> ProcessDisc(
        [FromQuery] Guid candidateId,
        [FromQuery] Guid discResultId,
        [FromQuery] Guid eventId,
        [FromQuery] Guid correlationId,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return UnauthorizedResponse();
        }

        var command = new ProcessDiscInterpretationCommand(candidateId, discResultId, eventId, correlationId);
        var result = await _discInterpretationHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("generate-report")]
    public async Task<IActionResult> GenerateReportWebhook(
        [FromQuery] Guid candidateId,
        [FromQuery] Guid eventId,
        [FromQuery] Guid correlationId,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return UnauthorizedResponse();
        }

        var command = new GenerateInterviewReportCommand(candidateId, eventId, correlationId);
        var result = await _reportGenerationHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result);
    }

    private bool IsAuthorized()
    {
        if (User.Identity?.IsAuthenticated == true && (User.IsInRole("ats_admin") || User.IsInRole("ats_recruiter")))
        {
            return true;
        }

        string? expectedSecret = _configuration["Webhooks:Secret"] ?? _configuration["Ingestion:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedSecret))
        {
            return false;
        }

        var expectedSecretBytes = System.Text.Encoding.UTF8.GetBytes(expectedSecret);

        if (Request.Headers.TryGetValue("X-Webhook-Secret", out var headerSecret) &&
            !string.IsNullOrWhiteSpace(headerSecret))
        {
            var headerBytes = System.Text.Encoding.UTF8.GetBytes(headerSecret.ToString());
            if (System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(headerBytes, expectedSecretBytes))
            {
                return true;
            }
        }

        if (Request.Headers.TryGetValue("X-Api-Key", out var headerKey) &&
            !string.IsNullOrWhiteSpace(headerKey))
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(headerKey.ToString());
            if (System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(keyBytes, expectedSecretBytes))
            {
                return true;
            }
        }

        return false;
    }

    private IActionResult UnauthorizedResponse()
    {
        return StatusCode(StatusCodes.Status401Unauthorized, new ProblemDetails
        {
            Title = "Unauthorized",
            Detail = "Autenticación requerida para ejecutar webhooks. Proporcione un Bearer Token válido o la cabecera X-Webhook-Secret.",
            Status = StatusCodes.Status401Unauthorized
        });
    }
}
