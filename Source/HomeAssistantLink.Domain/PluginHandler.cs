namespace HomeAssistantLink.Domain;

using HomeAssistantLink.Domain.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed partial class PluginHandler(
    IEnumerable<IPlugin> plugins,
    IPluginCommandCatalog pluginCommandCatalog,
    IUserPluginCommandClient userPluginCommandClient,
    IOptions<PluginHostConfig> hostConfig,
    ILogger<PluginHandler> logger) : IPluginHandler
{
    private readonly IEnumerable<IPlugin> plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
    private readonly IPluginCommandCatalog pluginCommandCatalog = pluginCommandCatalog ?? throw new ArgumentNullException(nameof(pluginCommandCatalog));

    private readonly IUserPluginCommandClient userPluginCommandClient =
        userPluginCommandClient ?? throw new ArgumentNullException(nameof(userPluginCommandClient));

    private readonly PluginHostConfig hostConfig = hostConfig.Value ?? throw new ArgumentNullException(nameof(hostConfig));

    public void Handle(string entityId, string state)
    {
        var command = this.pluginCommandCatalog.GetCommand(entityId, state);

        if (command == null)
        {
            NoPluginCommandMatched(logger, entityId, state);
            return;
        }

        var plugin = this.plugins.FirstOrDefault(currentPlugin => currentPlugin.CanExecute(entityId, state));

        if (plugin == null)
        {
            NoPluginMatched(logger, entityId, state);
            return;
        }

        if (command.RunAs == this.hostConfig.RunAs)
        {
            PluginMatchedForCurrentHost(logger, entityId, state, command.RunAs);
            plugin.Execute(entityId, state);
            return;
        }

        if (this.hostConfig.RunAs == PluginRunAs.System &&
            command.RunAs == PluginRunAs.User)
        {
            ForwardingUserPluginCommand(logger, command.PluginType, command.CommandId, entityId, state);
            this.userPluginCommandClient.Handle(command);
            return;
        }

        IgnoringPluginCommandForDifferentHost(logger, entityId, state, this.hostConfig.RunAs, command.RunAs);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "No plugin matched command. EntityId: {EntityId}, State: {State}")]
    private static partial void NoPluginMatched(
        ILogger logger,
        string entityId,
        string state);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Plugin command matched current host. EntityId: {EntityId}, State: {State}, RunAs: {RunAs}")]
    private static partial void PluginMatchedForCurrentHost(
        ILogger logger,
        string entityId,
        string state,
        PluginRunAs runAs);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Forwarding user plugin command. PluginType: {PluginType}, CommandId: {CommandId}, EntityId: {EntityId}, State: {State}")]
    private static partial void ForwardingUserPluginCommand(
        ILogger logger,
        string pluginType,
        string commandId,
        string entityId,
        string state);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Ignoring plugin command because it targets a different host. EntityId: {EntityId}, State: {State}, CurrentHost: {CurrentHost}, RequiredHost: {RequiredHost}")]
    private static partial void IgnoringPluginCommandForDifferentHost(
        ILogger logger,
        string entityId,
        string state,
        PluginRunAs currentHost,
        PluginRunAs requiredHost);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "No plugin command metadata matched. EntityId: {EntityId}, State: {State}")]
    private static partial void NoPluginCommandMatched(
        ILogger logger,
        string entityId,
        string state);
}
