namespace HomeAssistantLink.Monitors.Display;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Monitors.Display.Contracts;
using HomeAssistantLink.UserSession.Contracts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddDisplayMonitor(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<DisplayMonitorConfig>(
            builder.Configuration.GetSection("HomeAssistantLink:Monitors:DisplayMonitor"));

        builder.Services.AddSingleton<DisplayMonitor>();

        builder.Services.AddSingleton<IMonitor>(services =>
                                                    services.GetRequiredService<DisplayMonitor>());

        builder.Services.AddSingleton<IUserSessionEventSink>(services =>
                                                                    services.GetRequiredService<DisplayMonitor>());

        return builder;
    }
}
