using Microsoft.AspNetCore.Http;
using team_hub_auth.Configuration;
using Xunit;

namespace team_hub_auth.Tests.Configuration;

public sealed class SessionIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PreservesIncomingHeader()
    {
        const string sessionId = "visit-session-id";
        var context = new DefaultHttpContext();
        context.Request.Headers[SessionIdMiddleware.HeaderName] = sessionId;

        var middleware = new SessionIdMiddleware(async context =>
        {
            await context.Response.WriteAsync("ok");
        });
        await middleware.InvokeAsync(context);

        Assert.Equal(sessionId, context.Request.Headers[SessionIdMiddleware.HeaderName].ToString());
        Assert.Equal(sessionId, context.Items[SessionIdMiddleware.ItemKey]);
        Assert.Equal(sessionId, context.Response.Headers[SessionIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_GeneratesHeader_WhenMissing()
    {
        var context = new DefaultHttpContext();

        var middleware = new SessionIdMiddleware(async context =>
        {
            await context.Response.WriteAsync("ok");
        });
        await middleware.InvokeAsync(context);

        var sessionId = context.Request.Headers[SessionIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(sessionId));
        Assert.True(Guid.TryParse(sessionId, out _));
        Assert.Equal(sessionId, context.Items[SessionIdMiddleware.ItemKey]);
        Assert.Equal(sessionId, context.Response.Headers[SessionIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_GeneratesHeader_WhenEmpty()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[SessionIdMiddleware.HeaderName] = "   ";

        var middleware = new SessionIdMiddleware(async context =>
        {
            await context.Response.WriteAsync("ok");
        });
        await middleware.InvokeAsync(context);

        var sessionId = context.Request.Headers[SessionIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(sessionId));
        Assert.True(Guid.TryParse(sessionId, out _));
    }
}
