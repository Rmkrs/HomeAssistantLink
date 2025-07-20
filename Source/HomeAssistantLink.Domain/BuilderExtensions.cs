namespace HomeAssistantLink.Domain;

using HomeAssistantLink.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddDomain(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IMonitorHandler, MonitorHandler>();
        builder.Services.AddSingleton<IPluginHandler, PluginHandler>();
        return builder;
    }
}