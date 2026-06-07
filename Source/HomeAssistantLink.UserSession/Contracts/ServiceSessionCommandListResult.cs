namespace HomeAssistantLink.UserSession.Contracts;

using HomeAssistantLink.Domain.Contracts;

public sealed class ServiceSessionCommandListResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public IReadOnlyList<PluginCommand> Commands { get; set; } = [];
}
