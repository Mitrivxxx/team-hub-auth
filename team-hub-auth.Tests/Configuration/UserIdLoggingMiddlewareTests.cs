using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using team_hub_auth.Configuration;
using Xunit;

namespace team_hub_auth.Tests.Configuration;

public sealed class UserIdLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_DoesNotSetUserId_WhenUnauthenticated()
    {
        var context = new DefaultHttpContext();
        var middleware = new UserIdLoggingMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.False(context.Items.ContainsKey(UserIdLoggingMiddleware.ItemKey));
    }

    [Fact]
    public async Task InvokeAsync_SetsUserId_FromSubClaim()
    {
        const string userId = "019f5fe8-2b37-749d-8680-009f8cc5e67f";
        var context = CreateAuthenticatedContext(new Claim(JwtRegisteredClaimNames.Sub, userId));
        var middleware = new UserIdLoggingMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(userId, context.Items[UserIdLoggingMiddleware.ItemKey]);
    }

    [Fact]
    public async Task InvokeAsync_SetsUserId_FromNameIdentifierClaim()
    {
        const string userId = "019f5fe8-2b37-749d-8680-009f8cc5e67f";
        var context = CreateAuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, userId));
        var middleware = new UserIdLoggingMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(userId, context.Items[UserIdLoggingMiddleware.ItemKey]);
    }

    static DefaultHttpContext CreateAuthenticatedContext(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Bearer");
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        return context;
    }
}
