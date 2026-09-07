using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Ats.Domain.Common;

namespace Ats.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Type = "https://api.ats.com/errors/internal-server-error",
            Title = "Error interno del servidor",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = _env.IsDevelopment()
                ? exception.Message
                : "Ha ocurrido un error inesperado al procesar su solicitud.",
            Instance = context.Request.Path
        };

        problemDetails.Extensions["correlationId"] = context.TraceIdentifier;

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}

