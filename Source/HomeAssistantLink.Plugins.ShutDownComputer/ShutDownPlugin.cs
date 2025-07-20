namespace HomeAssistantLink.Plugins.ShutDownComputer;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Plugins.ShutDownComputer.Contracts;
using Microsoft.Extensions.Options;

public class ShutDownPlugin(IOptions<ShutdownPluginConfig> options, IShutdownInvoker shutdownInvoker) : IPlugin
{
    private readonly ShutdownPluginConfig options = options == null ? throw new ArgumentNullException(nameof(options)) : options.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly IShutdownInvoker shutdownInvoker = shutdownInvoker ?? throw new ArgumentNullException(nameof(shutdownInvoker));

    public void Execute(String entityId, String state)
    {
        if (entityId != this.options.EntityId)
        {
            return;
        }

        if (state != this.options.Command)
        {
            return;
        }

        this.shutdownInvoker.Invoke();
    }
}