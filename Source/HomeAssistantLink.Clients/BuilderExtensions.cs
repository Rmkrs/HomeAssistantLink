namespace HomeAssistantLink.Clients;

using HomeAssistantLink.Clients.Contracts;
using HomeAssistantLink.Domain.Contracts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddClients(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<RestApiClientSettings>(
            builder.Configuration.GetSection("HomeAssistantLink:HomeAssistant"));

        builder.Services
            .AddHttpClient<ISetEntityState, RestApiClient>();

        return builder;
    }
}