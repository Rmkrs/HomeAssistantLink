namespace HomeAssistantLink.UserSession;

using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.UserSession.Contracts;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed partial class ServiceSessionCommandServer(
    IPluginCommandCatalog pluginCommandCatalog,
    IPluginHandler pluginHandler,
    IOptions<NamedPipeServiceSessionConfig> options,
    ILogger<ServiceSessionCommandServer> logger) : BackgroundService
{
    private readonly NamedPipeServiceSessionConfig options = options.Value ?? throw new ArgumentNullException(nameof(options));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ServiceSessionServerStarted(logger, this.options.PipeName);

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
            await using var writer = new StreamWriter(stream, leaveOpen: true);
            writer.AutoFlush = true;

            var payload = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(payload))
            {
                await WriteResponseAsync(
                    writer,
                    new ServiceSessionResponseModel
                    {
                        Success = false,
                        Error = "Empty request payload.",
                    },
                    cancellationToken).ConfigureAwait(false);

                return;
            }

            var request = JsonSerializer.Deserialize<ServiceSessionRequestModel>(payload);

            if (request == null)
            {
                await WriteResponseAsync(
                    writer,
                    new ServiceSessionResponseModel
                    {
                        Success = false,
                        Error = "Invalid request payload.",
                    },
                    cancellationToken).ConfigureAwait(false);

                return;
            }

            var response = this.HandleRequest(request);

            await WriteResponseAsync(writer, response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (Exception exception)
        {
            ServiceSessionRequestFailed(logger, exception);
        }
    }

    private ServiceSessionResponseModel HandleRequest(ServiceSessionRequestModel request)
    {
        switch (request.RequestType)
        {
            case ServiceSessionRequestType.GetUserCommands:
                return new ServiceSessionResponseModel
                {
                    Success = true,
                    Commands = [.. pluginCommandCatalog.GetUserCommands()],
                };

            case ServiceSessionRequestType.ExecuteCommand:
                if (string.IsNullOrWhiteSpace(request.CommandId))
                {
                    return new ServiceSessionResponseModel
                    {
                        Success = false,
                        Error = "CommandId is required.",
                    };
                }

                var command = pluginCommandCatalog.GetUserCommand(request.CommandId);

                if (command == null)
                {
                    return new ServiceSessionResponseModel
                    {
                        Success = false,
                        Error = $"User command '{request.CommandId}' was not found.",
                    };
                }

                ServiceSessionExecuteCommandReceived(
                    logger,
                    command.CommandId,
                    command.EntityId,
                    command.State);

                pluginHandler.Handle(command.EntityId, command.State);

                return new ServiceSessionResponseModel
                {
                    Success = true,
                };

            default:
                return new ServiceSessionResponseModel
                {
                    Success = false,
                    Error = $"Unsupported request type '{request.RequestType}'.",
                };
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
        ServiceSessionResponseModel response,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(response);

        await writer.WriteLineAsync(payload.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Service session command server started. PipeName: {PipeName}")]
    private static partial void ServiceSessionServerStarted(
        ILogger logger,
        string pipeName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Service session request failed.")]
    private static partial void ServiceSessionRequestFailed(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Service session execute command received. CommandId: {CommandId}, EntityId: {EntityId}, State: {State}")]
    private static partial void ServiceSessionExecuteCommandReceived(
        ILogger logger,
        string commandId,
        string entityId,
        string state);
}
