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
}
