using Microsoft.AspNetCore.Mvc;
using team_hub_auth.Exceptions;

namespace team_hub_auth.Configuration;

public sealed class ExceptionMiddleware(
    RequestDelegate next,
    IHostEnvironment environment,
    ILogger<ExceptionMiddleware> logger)
{
    const string GenericErrorDetail = "An unexpected error occurred. Please contact administrator.";
    const string RedisUnavailableDetail = "Authentication service temporarily unavailable. Please try again later.";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(ex, "Unhandled exception after response started");
                throw;
            }

            var correlationId = ResolveCorrelationId(context);
            logger.LogError(
                ex,
                "Unhandled exception for request {Method} {Path} (CorrelationId: {CorrelationId})",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            var problem = BuildProblemDetails(context, ex, correlationId);
            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problem);
            context.Response.ContentType = "application/problem+json; charset=utf-8";
        }
    }

    static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var item) &&
            item is string correlationIdFromItems &&
            !string.IsNullOrWhiteSpace(correlationIdFromItems))
        {
            return correlationIdFromItems;
        }

        var correlationIdFromHeader = context.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(correlationIdFromHeader))
        {
            return correlationIdFromHeader;
        }

        return Guid.NewGuid().ToString();
    }

    ProblemDetails BuildProblemDetails(HttpContext context, Exception exception, string correlationId)
    {
        var (statusCode, title, detail) = MapException(exception);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = environment.IsDevelopment() ? exception.Message : detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path
        };

        problem.Extensions["correlationId"] = correlationId;

        if (context.Items.TryGetValue(SessionIdMiddleware.ItemKey, out var sessionId) &&
            sessionId is string sessionIdValue &&
            !string.IsNullOrWhiteSpace(sessionIdValue))
        {
            problem.Extensions["sessionId"] = sessionIdValue;
        }

        if (environment.IsDevelopment())
        {
            problem.Extensions["stackTrace"] = exception.ToString();
        }

        return problem;
    }

    static (int StatusCode, string Title, string Detail) MapException(Exception exception) =>
        exception switch
        {
            RedisUnavailableException => (
                StatusCodes.Status503ServiceUnavailable,
                "Authentication service temporarily unavailable",
                RedisUnavailableDetail),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                GenericErrorDetail)
        };
}
