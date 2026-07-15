using Serilog.Context;

namespace team_hub_auth.Configuration;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Request.Headers[HeaderName] = correlationId;
        context.Items[ItemKey] = correlationId;
        context.Response.Headers.Append(HeaderName, correlationId);

        using (LogContext.PushProperty(ItemKey, correlationId))
        {
            await next(context);
        }
    }
}
