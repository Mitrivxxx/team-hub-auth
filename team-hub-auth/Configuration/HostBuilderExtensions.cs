using Serilog;

namespace team_hub_auth.Configuration;

public static class HostBuilderExtensions
{
    public static IHostBuilder AddSerilogConfiguration(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, services, configuration) =>
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));
        return hostBuilder;
    }
}
