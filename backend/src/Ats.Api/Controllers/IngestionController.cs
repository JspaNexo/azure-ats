using Microsoft.AspNetCore.Mvc;
using Ats.Application.DTOs;
using Ats.Application.Features.Ingestion;

namespace Ats.Api.Controllers;

public class IngestCandidateFormRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? TargetRole { get; set; }
    public Guid? JobPositionId { get; set; }
    public IFormFile File { get; set; } = default!;
    public int Dominance { get; set; } = 50;
    public int Influence { get; set; } = 50;
    public int Steadiness { get; set; } = 50;
    public int Conscientiousness { get; set; } = 50;
    public string? PrimaryStyle { get; set; }
    public bool Async { get; set; } = false;
}

[Route("api/v1/ingestion")]
public class IngestionController : ApiControllerBase
{
    private readonly IngestCandidateCommandHandler _ingestHandler;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IngestionController> _logger;

    public IngestionController(
        IngestCandidateCommandHandler ingestHandler,
        IConfiguration configuration,
        ILogger<IngestionController> logger)
    {
        _ingestHandler = ingestHandler;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint unificado de ingesta y evaluación para servicios externos y n8n.
    /// Recibe los datos del postulante, su archivo CV en PDF y los puntajes DISC,
    /// y ejecuta todo el pipeline de análisis con Google Gemini AI.
    /// </summary>
    [HttpPost("evaluate")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CandidateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EvaluateCandidate(
        [FromForm] IngestCandidateFormRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Dual authentication check: Bearer token with roles OR X-Api-Key header
        if (!IsAuthorized())
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Autenticación requerida para evaluar candidatos. Proporcione un Bearer Token válido con rol autorizado o la cabecera X-Api-Key.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "File.Empty",
                Detail = "Debe adjuntar un archivo CV en formato PDF válido.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // 2. File size limit (15 MB)
        if (request.File.Length > 15 * 1024 * 1024)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "File.TooLarge",
                Detail = "El archivo excede el tamaño máximo permitido de 15 MB.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!request.File.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "File.InvalidFormat",
                Detail = "Solo se permiten archivos con extensión .pdf.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        using var stream = request.File.OpenReadStream();

        // 3. Magic bytes validation (%PDF- -> 0x25, 0x50, 0x44, 0x46)
        byte[] magicHeader = new byte[4];
        int bytesRead = await stream.ReadAsync(magicHeader, 0, 4, cancellationToken);
        if (bytesRead < 4 || magicHeader[0] != 0x25 || magicHeader[1] != 0x50 || magicHeader[2] != 0x44 || magicHeader[3] != 0x46)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "File.InvalidMagicBytes",
                Detail = "El archivo suministrado no contiene una cabecera binaria válida de documento PDF (%PDF).",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        _logger.LogInformation(
            "Iniciando ingesta y evaluación con IA para candidato: {Email}, Puesto sugerido: {TargetRole}",
            request.Email,
            request.TargetRole ?? "No especificado");
        var command = new IngestCandidateCommand(
            FirstName: request.FirstName,
            LastName: request.LastName,
            Email: request.Email,
            PhoneNumber: request.PhoneNumber,
            CvStream: stream,
            FileName: request.File.FileName,
            FileSizeBytes: request.File.Length,
            ContentType: request.File.ContentType,
            Dominance: request.Dominance,
            Influence: request.Influence,
            Steadiness: request.Steadiness,
            Conscientiousness: request.Conscientiousness,
            PrimaryStyle: request.PrimaryStyle,
            TargetRole: request.TargetRole,
            JobPositionId: request.JobPositionId,
            Asynchronous: request.Async);

        var result = await _ingestHandler.HandleAsync(command, cancellationToken);
        if (request.Async && result.IsSuccess)
        {
            return Accepted($"/api/v1/candidates/{result.Value.Id}", result.Value);
        }
        return HandleResult(result);
    }

    private bool IsAuthorized()
    {
        // Check Bearer Token claims
        if (User.Identity?.IsAuthenticated == true && (User.IsInRole("ats_admin") || User.IsInRole("ats_recruiter")))
        {
            return true;
        }

        // Check external integration API Key header
        string? expectedApiKey = _configuration["Ingestion:ApiKey"];
        
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            return false;
        }

        if (Request.Headers.TryGetValue("X-Api-Key", out var headerKey) &&
            !string.IsNullOrWhiteSpace(headerKey))
        {
            var headerBytes = System.Text.Encoding.UTF8.GetBytes(headerKey.ToString());
            var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expectedApiKey);
            
            if (System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(headerBytes, expectedBytes))
            {
                return true;
            }
        }

        return false;
    }
}
