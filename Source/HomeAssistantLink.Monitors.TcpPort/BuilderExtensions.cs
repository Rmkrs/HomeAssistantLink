namespace HomeAssistantLink.Monitors.TcpPort;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.TcpPort.Contracts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddTcpPortMonitor(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IMonitor, TcpPortMonitor>();
        builder.Services.Configure<TcpPortMonitorConfig>(
            builder.Configuration.GetSection("HomeAssistantLink:Monitors:TcpPortMonitor"));

        return builder;
    }
}
