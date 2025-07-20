namespace HomeAssistantLink.Plugins.ShutDownComputer.Contracts;

public class ShutdownPluginConfig
{
    public string EntityId { get; set; } = String.Empty;

    public string Command { get; set; } = String.Empty;
}