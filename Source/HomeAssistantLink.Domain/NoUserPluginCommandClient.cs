namespace HomeAssistantLink.Domain;

using HomeAssistantLink.Domain.Contracts;

using Microsoft.Extensions.Logging;

public sealed partial class NoUserPluginCommandClient(
    ILogger<NoUserPluginCommandClient> logger) : IUserPluginCommandClient
{
    public void Handle(PluginCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        UserPluginCommandIgnored(
            logger,
            command.PluginType,
            command.EntityId,
            command.State);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "User plugin command was approved but no user plugin command client is registered. PluginType: {PluginType}, EntityId: {EntityId}, State: {State}")]
    private static partial void UserPluginCommandIgnored(
        ILogger logger,
        string pluginType,
        string entityId,
        string state);
}
