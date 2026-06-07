namespace HomeAssistantLink.Domain.Contracts;

public interface IPluginCommandCatalog
{
    IReadOnlyList<PluginCommand> GetUserCommands();

    PluginCommand? GetCommand(string entityId, string state);

    PluginCommand? GetUserCommand(string commandId);
}
