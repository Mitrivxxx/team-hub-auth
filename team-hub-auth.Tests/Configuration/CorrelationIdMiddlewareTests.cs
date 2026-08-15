using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using TeamHub.Observability.Middleware;
using Xunit;

namespace team_hub_auth.Tests.Configuration;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PreservesIncomingHeader()
    {
        const string correlationId = "gateway-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

        var middleware = new CorrelationIdMiddleware(async context =>
        {
            await context.Response.WriteAsync("ok");
        });
        await middleware.InvokeAsync(context);

        Assert.Equal(correlationId, context.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString());
        Assert.Equal(correlationId, context.Items[CorrelationIdMiddleware.ItemKey]);
        Assert.Equal(correlationId, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_GeneratesHeader_WhenMissing()
    {
        var context = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(async context =>
        {
            await context.Response.WriteAsync("ok");
        });
        await middleware.InvokeAsync(context);

        var correlationId = context.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _));
        Assert.Equal(correlationId, context.Items[CorrelationIdMiddleware.ItemKey]);
        Assert.Equal(correlationId, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_GeneratesHeader_WhenEmpty()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = string.Empty;

        var middleware = new CorrelationIdMiddleware(async context =>
        {
            await context.Response.WriteAsync("ok");
        });
        await middleware.InvokeAsync(context);

        var correlationId = context.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task InvokeAsync_GeneratesHeader_WhenWhitespace()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "   ";

        var middleware = new CorrelationIdMiddleware(async context =>
        {
            await context.Response.WriteAsync("ok");
        });
        await middleware.InvokeAsync(context);

        var correlationId = context.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task InvokeAsync_TrimsIncomingHeader()
    {
        const string correlationId = "trimmed-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = $"  {correlationId}  ";

        var middleware = new CorrelationIdMiddleware(async context =>
        {
            await context.Response.WriteAsync("ok");
        });
        await middleware.InvokeAsync(context);

        Assert.Equal(correlationId, context.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString());
        Assert.Equal(correlationId, context.Items[CorrelationIdMiddleware.ItemKey]);
    }

    [Fact]
    public async Task InvokeAsync_PrefersActiveTraceId_OverIncomingHeader()
    {
        const string incomingHeader = "client-uuid";
        using var activity = new Activity("correlation-test");
        activity.Start();

        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = incomingHeader;

        var middleware = new CorrelationIdMiddleware(async httpContext =>
        {
            await httpContext.Response.WriteAsync("ok");
        });
        await middleware.InvokeAsync(context);

        var expected = activity.TraceId.ToString();
        Assert.Equal(expected, context.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString());
        Assert.Equal(expected, context.Items[CorrelationIdMiddleware.ItemKey]);
        Assert.Equal(expected, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }
}
