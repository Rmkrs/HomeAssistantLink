namespace HomeAssistantLink.Domain;

using HomeAssistantLink.Domain.Contracts;

public sealed class PluginCommandCatalog(IEnumerable<IPlugin> plugins) : IPluginCommandCatalog
{
    private readonly IEnumerable<IPlugin> plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));

    public IReadOnlyList<PluginCommand> GetUserCommands()
    {
        return [.. this.GetCommands()
            .Where(command => command.RunAs == PluginRunAs.User)
            .OrderBy(command => command.DisplayName, StringComparer.OrdinalIgnoreCase)];
    }

    public PluginCommand? GetCommand(string entityId, string state)
    {
        return this.GetCommands()
            .FirstOrDefault(command =>
                                string.Equals(command.EntityId, entityId, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(command.State, state, StringComparison.OrdinalIgnoreCase));
    }

    public PluginCommand? GetUserCommand(string commandId)
    {
        return this.GetUserCommands()
            .FirstOrDefault(command =>
                                string.Equals(command.CommandId, commandId, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<PluginCommand> GetCommands()
    {
        return this.plugins.SelectMany(plugin => plugin.GetCommands());
    }
}
