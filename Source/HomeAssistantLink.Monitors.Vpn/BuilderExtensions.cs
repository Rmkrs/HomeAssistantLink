namespace HomeAssistantLink.Monitors.Vpn;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.Vpn.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddVpnMonitor(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<INetworkAdapterChangeMonitor, NetworkAdapterChangeMonitor>();
        builder.Services.AddSingleton<IMonitor, VpnMonitor>();
        builder.Services.Configure<VpnMonitorConfig>(builder.Configuration.GetSection("HomeAssistantLink:Monitors:VpnMonitor"));

        return builder;
    }
}