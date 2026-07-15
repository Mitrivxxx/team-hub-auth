using Serilog.Context;

namespace team_hub_auth.Configuration;

public sealed class SessionIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Session-ID";
    public const string ItemKey = "SessionId";

    public async Task InvokeAsync(HttpContext context)
    {
        var sessionId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
        }

        context.Request.Headers[HeaderName] = sessionId;
        context.Items[ItemKey] = sessionId;
        context.Response.Headers.Append(HeaderName, sessionId);

        using (LogContext.PushProperty(ItemKey, sessionId))
        {
            await next(context);
        }
    }
}
