namespace HomeAssistantLink.Clients;

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using HomeAssistantLink.Clients.Contracts;
using HomeAssistantLink.Domain.Contracts;

using Microsoft.Extensions.Options;

public class RestApiClient : ISetEntityState
{
    private readonly HttpClient httpClient;

    public RestApiClient(
        HttpClient httpClient,
        IOptions<RestApiClientSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(settings);

        var restApiClientSettings = settings.Value ?? throw new ArgumentNullException(nameof(settings));

        if (string.IsNullOrWhiteSpace(restApiClientSettings.Host))
        {
            throw new ArgumentException("Home Assistant host is required.", nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(restApiClientSettings.ApiKey))
        {
            throw new ArgumentException("Home Assistant API key is required.", nameof(settings));
        }

        this.httpClient = httpClient;
        this.httpClient.BaseAddress = new Uri(NormalizeBaseAddress(restApiClientSettings.Host), UriKind.Absolute);
        this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", restApiClientSettings.ApiKey);
    }

    public Task SetAsync(EntityStateUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);

        return update.EntityType switch
        {
            HomeAssistantEntityType.Boolean => this.SetBoolAsync(update, cancellationToken),
            HomeAssistantEntityType.Text => this.SetStringAsync(update, cancellationToken),
            HomeAssistantEntityType.Number => this.SetNumberAsync(update, cancellationToken),
            HomeAssistantEntityType.DateTime => this.SetDateTimeAsync(update, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported Home Assistant entity type '{update.EntityType}'."),
        };
    }

    private static string NormalizeBaseAddress(string value)
    {
        var trimmedValue = value.TrimEnd('/');

        if (trimmedValue.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            trimmedValue = trimmedValue[..^4];
        }

        return $"{trimmedValue}/";
    }

    private Task SetBoolAsync(EntityStateUpdate update, CancellationToken cancellationToken)
    {
        var value = Convert.ToBoolean(update.Value, CultureInfo.InvariantCulture);
        var service = value ? "turn_on" : "turn_off";

        return this.CallServiceAsync("input_boolean", service, new { entity_id = update.EntityId }, cancellationToken);
    }

    private Task SetStringAsync(EntityStateUpdate update, CancellationToken cancellationToken)
    {
        var value = Convert.ToString(update.Value, CultureInfo.InvariantCulture) ?? string.Empty;

        return this.CallServiceAsync("input_text", "set_value", new
        {
            entity_id = update.EntityId,
            value,
        }, cancellationToken);
    }

    private Task SetNumberAsync(EntityStateUpdate update, CancellationToken cancellationToken)
    {
        var value = Convert.ToDouble(update.Value, CultureInfo.InvariantCulture);

        return this.CallServiceAsync("input_number", "set_value", new
        {
            entity_id = update.EntityId,
            value,
        }, cancellationToken);
    }

    private Task SetDateTimeAsync(EntityStateUpdate update, CancellationToken cancellationToken)
    {
        var value = update.Value switch
        {
            DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("o", CultureInfo.InvariantCulture),
            null => string.Empty,
            _ => Convert.ToString(update.Value, CultureInfo.InvariantCulture) ?? string.Empty,
        };

        return this.CallServiceAsync("input_text", "set_value", new
        {
            entity_id = update.EntityId,
            value,
        }, cancellationToken);
    }

    private async Task CallServiceAsync(
        string domain,
        string service,
        object data,
        CancellationToken cancellationToken)
    {
        var requestUri = new Uri($"api/services/{domain}/{service}", UriKind.Relative);

        using var response = await this.httpClient
            .PostAsJsonAsync(requestUri, data, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
}