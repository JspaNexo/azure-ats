using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ats.Application.DTOs;
using Ats.Application.Features.Candidates;
using Ats.Application.Features.CvProcessing;
using Ats.Application.Features.Disc;
using Ats.Application.Features.Documents;
using Ats.Application.Features.Reports;
using Ats.Domain.Common;

namespace Ats.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error.Type switch
        {
            ErrorType.NotFound => NotFound(CreateProblemDetails(result.Error, StatusCodes.Status404NotFound)),
            ErrorType.Validation => BadRequest(CreateProblemDetails(result.Error, StatusCodes.Status400BadRequest)),
            ErrorType.Conflict => Conflict(CreateProblemDetails(result.Error, StatusCodes.Status409Conflict)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result.Error, StatusCodes.Status500InternalServerError))
        };
    }

    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.Error.Type switch
        {
            ErrorType.NotFound => NotFound(CreateProblemDetails(result.Error, StatusCodes.Status404NotFound)),
            ErrorType.Validation => BadRequest(CreateProblemDetails(result.Error, StatusCodes.Status400BadRequest)),
            ErrorType.Conflict => Conflict(CreateProblemDetails(result.Error, StatusCodes.Status409Conflict)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result.Error, StatusCodes.Status500InternalServerError))
        };
    }

    protected ProblemDetails CreateProblemDetails(Error error, int statusCode)
    {
        return new ProblemDetails
        {
            Type = $"https://api.ats.com/errors/{error.Code.ToLowerInvariant()}",
            Title = error.Code,
            Status = statusCode,
            Detail = error.Description,
            Instance = HttpContext.Request.Path
        };
    }
}

[Authorize]
[Route("api/v1/candidates")]
public class CandidatesController : ApiControllerBase
{
    private readonly RegisterCandidateCommandHandler _registerHandler;
    private readonly GetCandidateByIdQueryHandler _getByIdHandler;
    private readonly GetCandidatesQueryHandler _getAllHandler;
    private readonly UpdateEvaluatorDecisionCommandHandler _updateDecisionHandler;
    private readonly AssignCandidateCommandHandler _assignHandler;
    private readonly GetRecruitersQueryHandler _getRecruitersHandler;
    private readonly RecordCvFeedbackCommandHandler? _feedbackHandler;

    public CandidatesController(
        RegisterCandidateCommandHandler registerHandler,
        GetCandidateByIdQueryHandler getByIdHandler,
        GetCandidatesQueryHandler getAllHandler,
        UpdateEvaluatorDecisionCommandHandler updateDecisionHandler,
        AssignCandidateCommandHandler assignHandler,
        GetRecruitersQueryHandler getRecruitersHandler,
        RecordCvFeedbackCommandHandler? feedbackHandler = null)
    {
        _registerHandler = registerHandler;
        _getByIdHandler = getByIdHandler;
        _getAllHandler = getAllHandler;
        _updateDecisionHandler = updateDecisionHandler;
        _assignHandler = assignHandler;
        _getRecruitersHandler = getRecruitersHandler;
        _feedbackHandler = feedbackHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CandidateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? recruiterId, CancellationToken cancellationToken)
    {
        var query = new GetCandidatesQuery(recruiterId);
        var result = await _getAllHandler.HandleAsync(query, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CandidateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterCandidateRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCandidateCommand(request.FirstName, request.LastName, request.Email, request.PhoneNumber);
        var result = await _registerHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CandidateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetCandidateByIdQuery(id);
        var result = await _getByIdHandler.HandleAsync(query, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/decision")]
    [ProducesResponseType(typeof(CandidateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDecision(
        [FromRoute] Guid id,
        [FromBody] UpdateEvaluatorDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEvaluatorDecisionCommand(id, request.Decision, request.Notes);
        var result = await _updateDecisionHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "ats_admin")]
    [HttpPatch("{id:guid}/assign")]
    [ProducesResponseType(typeof(CandidateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(
        [FromRoute] Guid id,
        [FromBody] AssignCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignCandidateCommand(id, request.RecruiterId, request.RecruiterName, request.RecruiterEmail);
        var result = await _assignHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "ats_admin")]
    [HttpGet("/api/v1/users/recruiters")]
    [ProducesResponseType(typeof(IReadOnlyList<RecruiterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecruiters(CancellationToken cancellationToken)
    {
        var query = new GetRecruitersQuery();
        var result = await _getRecruitersHandler.HandleAsync(query, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/cv-feedback")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordCvFeedback(
        [FromRoute] Guid id,
        [FromBody] CvFeedbackDto feedback,
        CancellationToken cancellationToken)
    {
        if (_feedbackHandler is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Servicio de feedback no configurado.");
        }

        var command = new RecordCvFeedbackCommand(id, feedback);
        var result = await _feedbackHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result);
    }
}

public class UploadCvFormRequest
{
    public Guid CandidateId { get; set; }
    public IFormFile File { get; set; } = default!;
}

[Authorize]
[Route("api/v1/documents")]
public class DocumentsController : ApiControllerBase
{
    private readonly UploadCvCommandHandler _uploadHandler;

    public DocumentsController(UploadCvCommandHandler uploadHandler)
    {
        _uploadHandler = uploadHandler;
    }

    [HttpPost("cv")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadCvResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadCv(
        [FromForm] UploadCvFormRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "File.Empty",
                Detail = "Debe proporcionar un archivo PDF válido.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        using var stream = request.File.OpenReadStream();
        var command = new UploadCvCommand(request.CandidateId, stream, request.File.FileName, request.File.Length, request.File.ContentType);
        var result = await _uploadHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("cv/{candidateId:guid}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCandidateCv(
        [FromRoute] Guid candidateId,
        [FromServices] GetCandidateCvDocumentQueryHandler queryHandler,
        CancellationToken cancellationToken)
    {
        var result = await queryHandler.HandleAsync(new GetCandidateCvDocumentQuery(candidateId), cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleResult(result);
        }

        var doc = result.Value;
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{doc.FileName}\"");
        return File(doc.Stream, doc.ContentType, enableRangeProcessing: true);
    }
}

[Authorize]
[Route("api/v1/disc")]
public class DiscController : ApiControllerBase
{
    private readonly SubmitDiscResultCommandHandler _submitHandler;

    public DiscController(SubmitDiscResultCommandHandler submitHandler)
    {
        _submitHandler = submitHandler;
    }

    [HttpPost("results")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitResult([FromBody] SubmitDiscResultRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitDiscResultCommand(
            request.CandidateId,
            request.Dominance,
            request.Influence,
            request.Steadiness,
            request.Conscientiousness,
            request.PrimaryStyle);

        var result = await _submitHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result);
    }
}

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
        // 1. Bearer token
        if (User.Identity?.IsAuthenticated == true && (User.IsInRole("ats_admin") || User.IsInRole("ats_recruiter")))
        {
            return true;
        }

        // 2. Secret webhook header
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



