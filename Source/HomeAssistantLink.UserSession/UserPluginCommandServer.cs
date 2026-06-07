namespace HomeAssistantLink.UserSession;

using System.IO.Pipes;
using System.Text.Json;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.UserSession.Contracts;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed partial class UserPluginCommandServer(
    IEnumerable<IUserPluginCommandExecutor> executors,
    IOptions<NamedPipeUserSessionConfig> options,
    ILogger<UserPluginCommandServer> logger) : BackgroundService
{
    private readonly IEnumerable<IUserPluginCommandExecutor> executors =
        executors ?? throw new ArgumentNullException(nameof(executors));

    private readonly NamedPipeUserSessionConfig options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        UserSessionServerStarted(logger, this.options.PipeName);

        while (!stoppingToken.IsCancellationRequested)
        {
            await this.HandleConnectionAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task HandleConnectionAsync(CancellationToken cancellationToken)
    {
        await using var stream = new NamedPipeServerStream(
            this.options.PipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        try
        {
            await stream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

            using var reader = new StreamReader(stream, leaveOpen: true);
            await using var writer = new StreamWriter(stream, leaveOpen: true)
            {
                AutoFlush = true,
            };

            var payload = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(payload))
            {
                await this.WriteResultAsync(
                    writer,
                    success: false,
                    error: "Empty command payload.",
                    cancellationToken).ConfigureAwait(false);

                return;
            }

            var command = JsonSerializer.Deserialize<PluginCommand>(payload);

            if (command == null)
            {
                await this.WriteResultAsync(
                    writer,
                    success: false,
                    error: "Invalid command payload.",
                    cancellationToken).ConfigureAwait(false);

                return;
            }

            UserSessionCommandReceived(logger, command.PluginType, command.EntityId, command.State);

            var executor = this.executors.FirstOrDefault(currentExecutor =>
                string.Equals(currentExecutor.PluginType, command.PluginType, StringComparison.OrdinalIgnoreCase));

            if (executor == null)
            {
                await this.WriteResultAsync(
                    writer,
                    success: false,
                    error: $"No user plugin executor registered for plugin type '{command.PluginType}'.",
                    cancellationToken).ConfigureAwait(false);

                return;
            }

            executor.Execute(command);

            await this.WriteResultAsync(
                writer,
                success: true,
                error: null,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (Exception exception)
        {
            UserSessionCommandFailed(logger, exception);
        }
    }

    private async Task WriteResultAsync(
        StreamWriter writer,
        bool success,
        string? error,
        CancellationToken cancellationToken)
    {
        var result = new UserPluginCommandResultModel
        {
            Success = success,
            Error = error,
        };

        var payload = JsonSerializer.Serialize(result);

        await writer.WriteLineAsync(payload.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "User session command server started. PipeName: {PipeName}")]
    private static partial void UserSessionServerStarted(
        ILogger logger,
        string pipeName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "User session command received. PluginType: {PluginType}, EntityId: {EntityId}, State: {State}")]
    private static partial void UserSessionCommandReceived(
        ILogger logger,
        string pluginType,
        string entityId,
        string state);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "User session command failed.")]
    private static partial void UserSessionCommandFailed(
        ILogger logger,
        Exception exception);
}