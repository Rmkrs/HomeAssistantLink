namespace HomeAssistantLink.Domain.Contracts;

public interface IPlugin
{
    IEnumerable<PluginCommand> GetCommands();

    bool CanExecute(string entityId, string state);

    void Execute(string entityId, string state);
}
