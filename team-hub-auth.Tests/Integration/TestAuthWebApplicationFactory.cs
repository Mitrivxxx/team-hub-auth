using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeamHub.Redis;
using team_hub_auth.Configuration;
using team_hub_auth.Tests.Configuration;

namespace team_hub_auth.Tests.Integration;

internal sealed class TestAuthWebApplicationFactory : WebApplicationFactory<Program>
{
    readonly string postgresConnectionString;
    readonly string redisConnectionString;
    readonly Action<IServiceCollection>? configureTestServices;

    public TestAuthWebApplicationFactory(
        string postgresConnectionString,
        string redisConnectionString,
        Action<IServiceCollection>? configureTestServices = null)
    {
        this.postgresConnectionString = postgresConnectionString;
        this.redisConnectionString = redisConnectionString;
        this.configureTestServices = configureTestServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:DefaultConnection", postgresConnectionString);
        builder.UseSetting($"{RedisOptions.SectionName}:ConnectionString", redisConnectionString);
        builder.UseSetting($"{JwtOptions.SectionName}:Key", TestJwtConfiguration.Key);
        builder.UseSetting($"{JwtOptions.SectionName}:Issuer", TestJwtConfiguration.Issuer);
        builder.UseSetting($"{JwtOptions.SectionName}:Audience", TestJwtConfiguration.Audience);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = postgresConnectionString,
                [$"{RedisOptions.SectionName}:ConnectionString"] = redisConnectionString,
                [$"{JwtOptions.SectionName}:Key"] = TestJwtConfiguration.Key,
                [$"{JwtOptions.SectionName}:Issuer"] = TestJwtConfiguration.Issuer,
                [$"{JwtOptions.SectionName}:Audience"] = TestJwtConfiguration.Audience,
            });
        });

        if (configureTestServices is not null)
        {
            builder.ConfigureServices(configureTestServices);
        }
    }
}
