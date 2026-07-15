using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using team_hub_auth.Exceptions;

namespace team_hub_auth.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception is RedisUnavailableException)
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Authentication service temporarily unavailable. Please try again later."
                    });
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Internal Server Error" });
            });
        });

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
