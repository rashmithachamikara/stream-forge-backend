using System.Text.Json;

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
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => 
                (StatusCodes.Status401Unauthorized, "Invalid credentials or unauthorized access."),
            ArgumentException => 
                (StatusCodes.Status400BadRequest, exception.Message),
            InvalidOperationException when exception.Message.Contains("not found") => 
                (StatusCodes.Status404NotFound, exception.Message),
            _ => 
                (StatusCodes.Status500InternalServerError, "An internal error occurred. Please try again later.")
        };

        context.Response.StatusCode = statusCode;

        var response = JsonSerializer.Serialize(new
        {
            error = message,
            statusCode,
            timestamp = DateTime.UtcNow
        });

        return context.Response.WriteAsync(response);
    }
}
