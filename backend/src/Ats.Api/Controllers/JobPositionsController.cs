using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ats.Application.Features.JobPositions;

namespace Ats.Api.Controllers;

[Authorize]
[Route("api/v1/positions")]
public class JobPositionsController : ApiControllerBase
{
    private readonly CreateJobPositionCommandHandler _createHandler;
    private readonly GetJobPositionsQueryHandler _getHandler;
    private readonly UpdateJobPositionStatusCommandHandler _updateStatusHandler;

    public JobPositionsController(
        CreateJobPositionCommandHandler createHandler,
        GetJobPositionsQueryHandler getHandler,
        UpdateJobPositionStatusCommandHandler updateStatusHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
        _updateStatusHandler = updateStatusHandler;
    }

    /// <summary>
    /// Lista todas las vacantes o filtra por estado (Active, Paused, Closed).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<JobPositionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPositions([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _getHandler.HandleAsync(new GetJobPositionsQuery(status), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Crea una nueva vacante de empleo. Exclusivo para administradores.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ats_admin")]
    [ProducesResponseType(typeof(JobPositionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePosition([FromBody] CreateJobPositionCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetPositions), new { id = result.Value.Id }, result.Value);
        }
        return HandleResult(result);
    }

    /// <summary>
    /// Actualiza el estado de una vacante (Active, Paused, Closed). Exclusivo para administradores.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "ats_admin")]
    [ProducesResponseType(typeof(JobPositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePositionStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _updateStatusHandler.HandleAsync(new UpdateJobPositionStatusCommand(id, request.Status), cancellationToken);
        return HandleResult(result);
    }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = "Active";
}