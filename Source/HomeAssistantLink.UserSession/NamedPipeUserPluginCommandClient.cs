namespace HomeAssistantLink.UserSession;

using System.IO.Pipes;
using System.Text.Json;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.UserSession.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed partial class NamedPipeUserPluginCommandClient(
    IOptions<NamedPipeUserSessionConfig> options,
    ILogger<NamedPipeUserPluginCommandClient> logger) : IUserPluginCommandClient
{
    private readonly NamedPipeUserSessionConfig options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public void Handle(PluginCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            using var stream = new NamedPipeClientStream(
                ".",
                this.options.PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            stream.Connect(this.options.ConnectTimeoutMilliseconds);

            using var writer = new StreamWriter(stream, leaveOpen: true)
            {
                AutoFlush = true,
            };

            using var reader = new StreamReader(stream, leaveOpen: true);

            var payload = JsonSerializer.Serialize(command);

            writer.WriteLine(payload);

            var responsePayload = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(responsePayload))
            {
                EmptyUserSessionResponse(logger, command.PluginType, command.EntityId, command.State);
                return;
            }

            var response = JsonSerializer.Deserialize<UserPluginCommandResultModel>(responsePayload);

            if (response?.Success == true)
            {
                UserSessionCommandCompleted(logger, command.PluginType, command.EntityId, command.State);
                return;
            }

            UserSessionCommandFailed(
                logger,
                command.PluginType,
                command.EntityId,
                command.State,
                response?.Error ?? "Unknown error.");
        }
        catch (TimeoutException exception)
        {
            UserSessionUnavailable(logger, exception, command.PluginType, command.EntityId, command.State);
        }
        catch (IOException exception)
        {
            UserSessionUnavailable(logger, exception, command.PluginType, command.EntityId, command.State);
        }
        catch (Exception exception)
        {
            UserSessionCommandUnexpectedFailure(logger, exception, command.PluginType, command.EntityId, command.State);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "User session did not return a response. PluginType: {PluginType}, EntityId: {EntityId}, State: {State}")]
    private static partial void EmptyUserSessionResponse(
        ILogger logger,
        string pluginType,
        string entityId,
        string state);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "User session command completed. PluginType: {PluginType}, EntityId: {EntityId}, State: {State}")]
    private static partial void UserSessionCommandCompleted(
        ILogger logger,
        string pluginType,
        string entityId,
        string state);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "User session command failed. PluginType: {PluginType}, EntityId: {EntityId}, State: {State}, Error: {Error}")]
    private static partial void UserSessionCommandFailed(
        ILogger logger,
        string pluginType,
        string entityId,
        string state,
        string error);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "User session is unavailable. PluginType: {PluginType}, EntityId: {EntityId}, State: {State}")]
    private static partial void UserSessionUnavailable(
        ILogger logger,
        Exception exception,
        string pluginType,
        string entityId,
        string state);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Unexpected failure while sending user session command. PluginType: {PluginType}, EntityId: {EntityId}, State: {State}")]
    private static partial void UserSessionCommandUnexpectedFailure(
        ILogger logger,
        Exception exception,
        string pluginType,
        string entityId,
        string state);
}