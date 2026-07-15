using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Serilog.Context;

namespace team_hub_auth.Configuration;

public sealed class UserIdLoggingMiddleware(RequestDelegate next)
{
    public const string ItemKey = "UserId";

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = ResolveUserId(context);
        if (userId is not null)
        {
            context.Items[ItemKey] = userId;
        }

        using (userId is not null ? LogContext.PushProperty(ItemKey, userId) : null)
        {
            await next(context);
        }
    }

    static string? ResolveUserId(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    }
}
