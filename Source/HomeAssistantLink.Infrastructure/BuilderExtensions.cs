namespace HomeAssistantLink.Infrastructure;

using HomeAssistantLink.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IDebounce, Debounce>();
        return builder;
    }
}