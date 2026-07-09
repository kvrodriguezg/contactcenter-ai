using System.Net;
using System.Text.Json;
using ContactCenterAI.Api.Models;
using FluentValidation;

namespace ContactCenterAI.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "No fue posible manejar la excepción porque la respuesta ya comenzó.");
            return;
        }

        var (statusCode, response) = MapException(exception);

        if (statusCode == (int)HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Error no controlado en la solicitud {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private (int StatusCode, ApiErrorResponse Response) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => (
                (int)HttpStatusCode.BadRequest,
                new ApiErrorResponse
                {
                    Message = "La solicitud contiene errores de validación.",
                    Errors = validationException.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(error => error.ErrorMessage).ToArray()),
                }),
            UnauthorizedAccessException unauthorizedException => (
                (int)HttpStatusCode.Unauthorized,
                new ApiErrorResponse
                {
                    Message = string.IsNullOrWhiteSpace(unauthorizedException.Message)
                        ? "No autorizado."
                        : unauthorizedException.Message,
                }),
            KeyNotFoundException notFoundException => (
                (int)HttpStatusCode.NotFound,
                new ApiErrorResponse
                {
                    Message = string.IsNullOrWhiteSpace(notFoundException.Message)
                        ? "Recurso no encontrado."
                        : notFoundException.Message,
                }),
            InvalidOperationException invalidOperationException when
                invalidOperationException.Message.Contains("no configurado", StringComparison.OrdinalIgnoreCase) ||
                invalidOperationException.Message.Contains("No fue posible generar", StringComparison.OrdinalIgnoreCase) => (
                (int)HttpStatusCode.ServiceUnavailable,
                new ApiErrorResponse
                {
                    Message = invalidOperationException.Message,
                }),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                new ApiErrorResponse
                {
                    Message = _environment.IsDevelopment()
                        ? exception.Message
                        : "Ha ocurrido un error interno.",
                }),
        };
    }
}
