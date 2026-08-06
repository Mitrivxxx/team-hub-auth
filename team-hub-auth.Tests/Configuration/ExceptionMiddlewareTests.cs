using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TeamHub.Observability;
using TeamHub.Observability.Middleware;
using team_hub_auth.Configuration;
using team_hub_auth.Exceptions;
using Xunit;

namespace team_hub_auth.Tests.Configuration;

public sealed class ExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Development_ReturnsProblemDetailsWithStackTrace()
    {
        const string correlationId = "dev-correlation-id";
        var context = CreateContext(correlationId);
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };
        var middleware = CreateMiddleware(
            environment,
            _ => throw new InvalidOperationException("Test global exception handling"));

        await middleware.InvokeAsync(context);

        var problem = await ReadProblemDetails(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        Assert.Equal(ProblemTypes.Internal, problem.GetProperty("type").GetString());
        Assert.Equal("Test global exception handling", problem.GetProperty("detail").GetString());
        Assert.Equal(correlationId, problem.GetProperty("correlationId").GetString());
        Assert.True(problem.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task InvokeAsync_Production_HidesStackTraceAndTechnicalDetail()
    {
        const string correlationId = "prod-correlation-id";
        var context = CreateContext(correlationId);
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var middleware = CreateMiddleware(
            environment,
            _ => throw new InvalidOperationException("Sensitive internal message"));

        await middleware.InvokeAsync(context);

        var problem = await ReadProblemDetails(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(
            "An unexpected error occurred. Please contact administrator.",
            problem.GetProperty("detail").GetString());
        Assert.Equal(correlationId, problem.GetProperty("correlationId").GetString());
        Assert.False(problem.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task InvokeAsync_RedisUnavailable_Returns503ProblemDetails()
    {
        var context = CreateContext("redis-correlation-id");
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var middleware = CreateMiddleware(
            environment,
            _ => throw new RedisUnavailableException("Redis session store is unavailable.", new Exception("inner")),
            new RedisUnavailableExceptionMapper());

        await middleware.InvokeAsync(context);

        var problem = await ReadProblemDetails(context);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal(ProblemTypes.ServiceUnavailable, problem.GetProperty("type").GetString());
        Assert.Equal("Authentication service temporarily unavailable", problem.GetProperty("title").GetString());
        Assert.Equal(
            "Authentication service temporarily unavailable. Please try again later.",
            problem.GetProperty("detail").GetString());
    }

    static DefaultHttpContext CreateContext(string correlationId)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new NullServiceProvider(),
            Response =
            {
                Body = new MemoryStream()
            }
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/auth/login";
        context.Items[CorrelationIdMiddleware.ItemKey] = correlationId;
        return context;
    }

    static ExceptionMiddleware CreateMiddleware(
        IHostEnvironment environment,
        RequestDelegate next,
        params IExceptionProblemDetailsMapper[] mappers) =>
        new(
            next,
            environment,
            NullLogger<ExceptionMiddleware>.Instance,
            mappers);

    static async Task<JsonElement> ReadProblemDetails(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }

    sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "team-hub-auth-tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(AppContext.BaseDirectory);
    }

    sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
