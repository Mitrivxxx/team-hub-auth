using Serilog;

namespace team_hub_auth.Configuration;

public static class HostBuilderExtensions
{
    public static IHostBuilder AddSerilogConfiguration(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));
        return hostBuilder;
    }
}
