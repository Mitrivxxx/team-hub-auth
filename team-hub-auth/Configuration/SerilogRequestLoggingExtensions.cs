using Serilog;
using Serilog.AspNetCore;
using Serilog.Events;

namespace team_hub_auth.Configuration;

public static class SerilogRequestLoggingExtensions
{
    public static IApplicationBuilder UseSerilogRequestLoggingExcludingHealth(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (context, _, exception) =>
                exception is not null
                    ? LogEventLevel.Error
                    : context.Request.Path.StartsWithSegments("/health")
                        ? LogEventLevel.Verbose
                        : LogEventLevel.Information;
        });
}
