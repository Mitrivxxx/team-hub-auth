using team_hub_auth.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogConfiguration();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddApiInfrastructure();
builder.Services.AddValidation();

var app = builder.Build();

await app.InitializeDatabaseAsync();
app.UseApiPipeline();

app.Run();
