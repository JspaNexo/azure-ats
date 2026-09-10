using Microsoft.AspNetCore.Mvc;
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
