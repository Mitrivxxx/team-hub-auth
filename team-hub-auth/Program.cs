using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using TeamHub.Observability;
using team_hub_auth.Configuration;
using team_hub_auth.Data;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Host.AddTeamHubSerilog();

builder.Services.AddTeamHubOpenTelemetry(builder.Configuration, "team-hub-auth", includeEntityFrameworkCore: true);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRedisSessionStore(builder.Configuration);
builder.Services.AddAuthHealthChecks(builder.Configuration);
builder.Services.AddJwtConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddApiInfrastructure();
builder.Services.AddValidation();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.Migrate();
}

app.UseApiPipeline();
app.MapTeamHubObservabilityEndpoints();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
