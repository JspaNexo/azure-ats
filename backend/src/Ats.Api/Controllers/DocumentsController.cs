using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ats.Application.DTOs;
using Ats.Application.Features.Documents;

namespace Ats.Api.Controllers;

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
