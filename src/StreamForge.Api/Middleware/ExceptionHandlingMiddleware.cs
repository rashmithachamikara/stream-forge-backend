using System.Text.Json;
using StreamForge.Domain.Exceptions;
using DomainUnauthorizedAccessException = StreamForge.Domain.Exceptions.UnauthorizedAccessException;
using DomainValidationException = StreamForge.Domain.Exceptions.ValidationException;

namespace StreamForge.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (exception is DomainException or ArgumentException)
            {
                _logger.LogWarning(exception, "Request failed: {Message}", exception.Message);
            }
            else
            {
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        if (exception is ExternalServiceThrottledException throttledException && throttledException.RetryAfter.HasValue)
        {
            context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(throttledException.RetryAfter.Value.TotalSeconds)).ToString();
        }

        var (statusCode, message) = exception switch
        {
            DomainUnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized, exception.Message),
            DomainValidationException =>
                (StatusCodes.Status400BadRequest, exception.Message),
            DuplicateEntityException =>
                (StatusCodes.Status409Conflict, exception.Message),
            EntityNotFoundException =>
                (StatusCodes.Status404NotFound, exception.Message),
            ExternalServiceThrottledException =>
                (StatusCodes.Status429TooManyRequests, exception.Message),
            ExternalServiceException =>
                (StatusCodes.Status503ServiceUnavailable, exception.Message),
            BusinessRuleViolationException =>
                (StatusCodes.Status400BadRequest, exception.Message),
            System.UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized, "Invalid credentials or unauthorized access."),
            ArgumentException => 
                (StatusCodes.Status400BadRequest, exception.Message),
            InvalidOperationException when exception.Message.Contains("not found") => 
                (StatusCodes.Status404NotFound, exception.Message),
            _ => 
                (StatusCodes.Status500InternalServerError, "An internal error occurred. Please try again later.")
        };

        context.Response.StatusCode = statusCode;

        if (exception is DomainValidationException { Errors.Count: > 0 } validationException)
        {
            var validationResponse = JsonSerializer.Serialize(new
            {
                error = message,
                errors = validationException.Errors,
                statusCode,
                timestamp = DateTime.UtcNow
            });

            return context.Response.WriteAsync(validationResponse);
        }

        var response = JsonSerializer.Serialize(new
        {
            error = message,
            statusCode,
            timestamp = DateTime.UtcNow
        });

        return context.Response.WriteAsync(response);
    }
}
