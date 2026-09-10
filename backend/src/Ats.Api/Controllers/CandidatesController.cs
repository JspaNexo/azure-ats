using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ats.Application.DTOs;
using Ats.Application.Features.Candidates;
using Ats.Application.Features.CvProcessing;

namespace Ats.Api.Controllers;

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
