namespace HomeAssistantLink.UserSession;

using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;

using HomeAssistantLink.UserSession.Contracts;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed partial class UserSessionEventServer(
    IEnumerable<IUserSessionEventSink> handlers,
    IOptions<NamedPipeUserSessionEventConfig> options,
    ILogger<UserSessionEventServer> logger) : BackgroundService
{
    private readonly IEnumerable<IUserSessionEventSink> handlers =
        handlers ?? throw new ArgumentNullException(nameof(handlers));

    private readonly NamedPipeUserSessionEventConfig options =
        options?.Value ?? throw new ArgumentNullException(nameof(options));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        UserSessionEventServerStarted(logger, this.options.PipeName);

        while (!stoppingToken.IsCancellationRequested)
        {
            await this.HandleConnectionAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task HandleConnectionAsync(CancellationToken cancellationToken)
    {
        await using var stream = NamedPipeServerStreamAcl.Create(
            pipeName: this.options.PipeName,
            direction: PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous,
            inBufferSize: 0,
            outBufferSize: 0,
            pipeSecurity: CreatePipeSecurity());

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
                await WriteResponseAsync(
                    writer,
                    success: false,
                    error: "Empty event payload.",
                    cancellationToken).ConfigureAwait(false);

                return;
            }

            var request = JsonSerializer.Deserialize<UserSessionEventRequestModel>(payload);

            if (request == null)
            {
                await WriteResponseAsync(
                    writer,
                    success: false,
                    error: "Invalid event payload.",
                    cancellationToken).ConfigureAwait(false);

                return;
            }

            UserSessionEventReceived(logger, request.EventType);

            foreach (var handler in this.handlers)
            {
                await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);
            }

            await WriteResponseAsync(
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
            UserSessionEventFailed(logger, exception);
        }
    }

    private static PipeSecurity CreatePipeSecurity()
    {
        var pipeSecurity = new PipeSecurity();

        pipeSecurity.AddAccessRule(new PipeAccessRule(
            identity: new SecurityIdentifier(WellKnownSidType.LocalSystemSid, domainSid: null),
            rights: PipeAccessRights.FullControl,
            type: AccessControlType.Allow));

        pipeSecurity.AddAccessRule(new PipeAccessRule(
            identity: new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, domainSid: null),
            rights: PipeAccessRights.FullControl,
            type: AccessControlType.Allow));

        pipeSecurity.AddAccessRule(new PipeAccessRule(
            identity: new SecurityIdentifier(WellKnownSidType.InteractiveSid, domainSid: null),
            rights: PipeAccessRights.ReadWrite,
            type: AccessControlType.Allow));

        return pipeSecurity;
    }

    private static async Task WriteResponseAsync(
        StreamWriter writer,
        bool success,
        string? error,
        CancellationToken cancellationToken)
    {
        var response = new UserSessionEventResponseModel
        {
            Success = success,
            Error = error,
        };

        var payload = JsonSerializer.Serialize(response);

        await writer.WriteLineAsync(payload.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "User session event server started. PipeName: {PipeName}")]
    private static partial void UserSessionEventServerStarted(
        ILogger logger,
        string pipeName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "User session event received. EventType: {EventType}")]
    private static partial void UserSessionEventReceived(
        ILogger logger,
        UserSessionEventType eventType);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "User session event failed.")]
    private static partial void UserSessionEventFailed(
        ILogger logger,
        Exception exception);
}
