namespace HomeAssistantLink.Monitors.WebCam;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.WebCam.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddWebCamMonitor(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IMonitor, WebCamMonitor>();
        builder.Services.AddSingleton<IWebCamIterator, WebCamIterator>();
        builder.Services.AddSingleton<IWebCamRegistryMonitor, WebCamRegistryMonitor>();
        builder.Services.Configure<WebCamMonitorConfig>(builder.Configuration.GetSection("HomeAssistantLink:Monitors:WebCamMonitor"));

        return builder;
    }
}