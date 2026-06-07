namespace HomeAssistantLink.Plugins.ShutDownComputer;

using HomeAssistantLink.Domain;
using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Plugins.ShutDownComputer.Contracts;

using Microsoft.Extensions.Options;

public class ShutDownPlugin(
    IOptions<ShutdownPluginConfig> options,
    IShutdownInvoker shutdownInvoker) : IPlugin
{
    public const string PluginType = "ShutDownComputer";

    private readonly ShutdownPluginConfig options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    private readonly IShutdownInvoker shutdownInvoker = shutdownInvoker ?? throw new ArgumentNullException(nameof(shutdownInvoker));

    public bool CanExecute(string entityId, string state)
    {
        return
            string.Equals(entityId, this.options.EntityId, StringComparison.Ordinal) &&
            string.Equals(state, this.options.Command, StringComparison.Ordinal);
    }

    public void Execute(string entityId, string state)
    {
        if (!this.CanExecute(entityId, state))
        {
            return;
        }

        this.shutdownInvoker.Invoke();
    }

    public IEnumerable<PluginCommand> GetCommands()
    {
        if (string.IsNullOrWhiteSpace(this.options.EntityId) ||
            string.IsNullOrWhiteSpace(this.options.Command))
        {
            return [];
        }

        return
        [
            new PluginCommand(
                CommandId: PluginCommandId.Create(PluginType, this.options.EntityId, this.options.Command),
                PluginType: PluginType,
                DisplayName: this.GetDisplayName(),
                EntityId: this.options.EntityId,
                State: this.options.Command,
                RunAs: this.options.RunAs,
                Parameters: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)),
        ];
    }

    private string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(this.options.DisplayName)
            ? this.options.Command
            : this.options.DisplayName;
    }
}
