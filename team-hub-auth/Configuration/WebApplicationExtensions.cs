using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using team_hub_auth.Exceptions;

namespace team_hub_auth.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<SessionIdMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<UserIdLoggingMiddleware>();
        app.UseSerilogRequestLoggingExcludingHealth();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapHealthChecks("/health");
        app.MapControllers();
        return app;
    }
}
