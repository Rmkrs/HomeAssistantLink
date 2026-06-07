namespace HomeAssistantLink.Domain.Contracts;

public sealed record PluginCommand(
    string CommandId,
    string PluginType,
    string DisplayName,
    string EntityId,
    string State,
    PluginRunAs RunAs,
    IReadOnlyDictionary<string, string> Parameters);
