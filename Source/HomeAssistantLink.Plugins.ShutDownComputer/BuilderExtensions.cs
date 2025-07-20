namespace HomeAssistantLink.Plugins.ShutDownComputer;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Plugins.ShutDownComputer.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddShutdownComputer(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IPlugin, ShutDownPlugin>();
        builder.Services.AddSingleton<IShutdownInvoker, ShutdownInvoker>();
        builder.Services.Configure<ShutdownPluginConfig>(builder.Configuration.GetSection("HomeAssistantLink:Plugins:ShutDownPlugin"));
        return builder;
    }
}