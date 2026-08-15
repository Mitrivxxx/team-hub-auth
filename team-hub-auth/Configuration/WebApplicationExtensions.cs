using Asp.Versioning.ApiExplorer;
using TeamHub.Observability;
using team_hub_auth.Grpc;

namespace team_hub_auth.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseTeamHubExceptionHandling();
        app.UseTeamHubCorrelationId();
        app.UseTeamHubSessionId();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseTeamHubUserIdLogging();
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
        app.MapGrpcService<UserProfileGrpcService>();
        return app;
    }
}
