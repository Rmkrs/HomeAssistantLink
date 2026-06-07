namespace HomeAssistantLink.Domain.Contracts;

public interface IUserPluginCommandExecutor
{
    string PluginType { get; }

    void Execute(PluginCommand command);
}
