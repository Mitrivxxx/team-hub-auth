using Asp.Versioning.ApiExplorer;
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
            app.UseSwaggerUI(options =>
            {
                var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"Team Hub Auth API {description.GroupName}");
                }
            });
        }

        app.MapHealthChecks("/health");
        app.MapControllers();
        return app;
    }
}
