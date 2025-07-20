namespace HomeAssistantLink.Domain;

using HomeAssistantLink.Domain.Contracts;

public class PluginHandler(IEnumerable<IPlugin> plugins) : IPluginHandler
{
    readonly IEnumerable<IPlugin> plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));

    public void Handle(string entityId, string state)
    {
        foreach (var plugin in this.plugins)
        {
            plugin.Execute(entityId, state);
        }
    }
}