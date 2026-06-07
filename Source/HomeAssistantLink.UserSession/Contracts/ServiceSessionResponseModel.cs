namespace HomeAssistantLink.UserSession.Contracts;

using HomeAssistantLink.Domain.Contracts;

public sealed class ServiceSessionResponseModel
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public List<PluginCommand> Commands { get; set; } = [];
}
