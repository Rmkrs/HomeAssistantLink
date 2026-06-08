namespace HomeAssistantLink.Monitors.Process;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.Process.Contracts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddProcessMonitor(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IMonitor, ProcessMonitor>();

        builder.Services.Configure<ProcessMonitorConfig>(
            builder.Configuration.GetSection("HomeAssistantLink:Monitors:ProcessMonitor"));

        return builder;
    }
}
