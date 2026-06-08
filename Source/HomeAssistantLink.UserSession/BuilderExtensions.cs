namespace HomeAssistantLink.UserSession;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.UserSession.Contracts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddUserSessionClient(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<NamedPipeUserSessionConfig>(
            builder.Configuration.GetSection("HomeAssistantLink:UserSession"));

        builder.Services.AddSingleton<IUserPluginCommandClient, NamedPipeUserPluginCommandClient>();

        return builder;
    }

    public static IHostApplicationBuilder AddUserSessionServer(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<NamedPipeUserSessionConfig>(
            builder.Configuration.GetSection("HomeAssistantLink:UserSession"));

        builder.Services.AddHostedService<UserPluginCommandServer>();

        return builder;
    }

    public static IHostApplicationBuilder AddServiceSessionClient(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<NamedPipeServiceSessionConfig>(
            builder.Configuration.GetSection("HomeAssistantLink:ServiceSession"));

        builder.Services.AddSingleton<IServiceSessionClient, NamedPipeServiceSessionClient>();

        return builder;
    }

    public static IHostApplicationBuilder AddServiceSessionServer(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<NamedPipeServiceSessionConfig>(
            builder.Configuration.GetSection("HomeAssistantLink:ServiceSession"));

        builder.Services.AddHostedService<ServiceSessionCommandServer>();

        return builder;
    }

    public static IHostApplicationBuilder AddUserSessionEventClient(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<NamedPipeUserSessionEventConfig>(
            builder.Configuration.GetSection("HomeAssistantLink:UserSessionEvents"));

        builder.Services.AddSingleton<IUserSessionEventClient, NamedPipeUserSessionEventClient>();

        return builder;
    }

    public static IHostApplicationBuilder AddUserSessionEventServer(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<NamedPipeUserSessionEventConfig>(
            builder.Configuration.GetSection("HomeAssistantLink:UserSessionEvents"));

        builder.Services.AddHostedService<UserSessionEventServer>();

        return builder;
    }
}
