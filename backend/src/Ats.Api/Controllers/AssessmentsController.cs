using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ats.Application.DTOs;
using Ats.Application.Features.Assessments;

namespace Ats.Api.Controllers;

[Authorize]
[Route("api/v1/assessments")]
public class AssessmentsController : ApiControllerBase
{
    private readonly SubmitAssessmentResultCommandHandler _submitHandler;
    private readonly GetCandidateAssessmentQueryHandler _getQueryHandler;

    public AssessmentsController(
        SubmitAssessmentResultCommandHandler submitHandler,
        GetCandidateAssessmentQueryHandler getQueryHandler)
    {
        _submitHandler = submitHandler;
        _getQueryHandler = getQueryHandler;
    }

    [HttpPost("results")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitResult([FromBody] SubmitAssessmentResultRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitAssessmentResultCommand(
            request.CandidateId,
            request.AssessmentType,
            request.Dimensions,
            request.PrimaryStyle,
            request.RawResultsJson);

        var result = await _submitHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("candidate/{candidateId:guid}")]
    [ProducesResponseType(typeof(CandidateAssessmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCandidateId(Guid candidateId, CancellationToken cancellationToken)
    {
        var query = new GetCandidateAssessmentQuery(candidateId);
        var result = await _getQueryHandler.HandleAsync(query, cancellationToken);
        return HandleResult(result);
    }
}
