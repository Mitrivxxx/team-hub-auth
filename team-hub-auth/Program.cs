using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using team_hub_auth.Configuration;
using team_hub_auth.Data;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogConfiguration();

builder.Services.AddDatabase(builder.Configuration);
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

app.Run();
