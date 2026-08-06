using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using TeamHub.Observability;
using team_hub_auth.Configuration;
using team_hub_auth.Data;
using team_hub_auth.Seeding;
using team_hub_auth.Seeding.Abstractions;

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
builder.Services.AddDemoSeeding(builder.Configuration, builder.Environment);
builder.Services.AddApiInfrastructure();
builder.Services.AddValidation();

var app = builder.Build();

if (args.Contains("--seed"))
{
    if (!SeedServiceCollectionExtensions.CanRunDemoSeed(app.Environment, app.Configuration))
        throw new InvalidOperationException(
            "Demo seed is only allowed when ASPNETCORE_ENVIRONMENT is Development or Staging and Seed:Enabled is true.");

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<IEnvironmentDataSeeder>().SeedAsync();
    return;
}

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.Migrate();
}

app.UseApiPipeline();
app.MapTeamHubObservabilityEndpoints();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
