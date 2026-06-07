namespace HomeAssistantLink.Domain;

using HomeAssistantLink.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddDomain(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<PluginHostConfig>(
            builder.Configuration.GetSection("HomeAssistantLink:PluginHost"));

        builder.Services.AddSingleton<IMonitorHandler, MonitorHandler>();
        builder.Services.AddSingleton<IPluginHandler, PluginHandler>();
        builder.Services.AddSingleton<IUserPluginCommandClient, NoUserPluginCommandClient>();
        builder.Services.AddSingleton<IPluginCommandCatalog, PluginCommandCatalog>();

        return builder;
    }
}
