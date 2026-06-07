namespace HomeAssistantLink.Domain;

public static class PluginCommandId
{
    public static string Create(
        string pluginType,
        string entityId,
        string state)
    {
        return $"{pluginType}:{entityId}:{state}";
    }
}
