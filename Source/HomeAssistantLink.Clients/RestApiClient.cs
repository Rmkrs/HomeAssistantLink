namespace HomeAssistantLink.Clients;

using System.Globalization;
using HADotNet.Core;
using HADotNet.Core.Clients;
using HomeAssistantLink.Clients.Contracts;
using HomeAssistantLink.Domain.Contracts;
using Microsoft.Extensions.Options;

public class RestApiClient : ISetEntityState
{
    private readonly ServiceClient serviceClient;

    public RestApiClient(IOptions<RestApiClientSettings> settings)
    {
        ClientFactory.Initialize(settings.Value.Host, settings.Value.ApiKey);
        this.serviceClient = ClientFactory.GetClient<ServiceClient>();
    }

    public void SetBool(string entityId, bool value)
    {
        var service = value ? "turn_on" : "turn_off";
        this.CallService("input_boolean", service, new { entity_id = entityId });
    }

    public void SetString(string entityId, string value)
    {
        this.CallService("input_text", "set_value", new { entity_id = entityId, value });
    }

    public void SetNumber(string entityId, double value)
    {
        this.CallService("input_number", "set_value", new { entity_id = entityId, value });
    }

    public void SetDate(string entityId, DateTime value)
    {
        var iso = value.ToString("o", CultureInfo.InvariantCulture);
        this.CallService("input_text", "set_value", new { entity_id = entityId, value = iso });
    }

    private void CallService(string domain, string service, object data)
    {
        this.serviceClient.CallService(domain, service, data)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }
}
