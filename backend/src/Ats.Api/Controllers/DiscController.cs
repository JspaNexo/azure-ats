using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ats.Application.DTOs;
using Ats.Application.Features.Disc;

namespace Ats.Api.Controllers;

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
