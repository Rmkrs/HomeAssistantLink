namespace HomeAssistantLink.UserSession;

using System.IO.Pipes;
using System.Text.Json;

using HomeAssistantLink.UserSession.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed partial class NamedPipeServiceSessionClient(
    IOptions<NamedPipeServiceSessionConfig> options,
    ILogger<NamedPipeServiceSessionClient> logger) : IServiceSessionClient
{
    private readonly NamedPipeServiceSessionConfig options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public ServiceSessionCommandListResult GetUserCommands()
    {
        var response = this.Send(new ServiceSessionRequestModel
        {
            RequestType = ServiceSessionRequestType.GetUserCommands,
        });

        if (response.Success)
        {
            return new ServiceSessionCommandListResult
            {
                Success = true,
                Commands = response.Commands,
            };
        }

        var error = response.Error ?? "Unknown error.";

        ServiceSessionRequestFailed(logger, error);

        return new ServiceSessionCommandListResult
        {
            Success = false,
            Error = error,
        };
    }

    public void Execute(string commandId)
    {
        var response = this.Send(new ServiceSessionRequestModel
        {
            RequestType = ServiceSessionRequestType.ExecuteCommand,
            CommandId = commandId,
        });

        if (response.Success)
        {
            return;
        }

        ServiceSessionRequestFailed(logger, response.Error ?? "Unknown error.");
    }

    private ServiceSessionResponseModel Send(ServiceSessionRequestModel request)
    {
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

            var payload = JsonSerializer.Serialize(request);

            writer.WriteLine(payload);

            var responsePayload = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(responsePayload))
            {
                return new ServiceSessionResponseModel
                {
                    Success = false,
                    Error = "Service session returned an empty response.",
                };
            }

            return JsonSerializer.Deserialize<ServiceSessionResponseModel>(responsePayload) ??
                new ServiceSessionResponseModel
                {
                    Success = false,
                    Error = "Service session returned an invalid response.",
                };
        }
        catch (TimeoutException exception)
        {
            ServiceSessionUnavailable(logger, exception);

            return new ServiceSessionResponseModel
            {
                Success = false,
                Error = "Service session is unavailable.",
            };
        }
        catch (IOException exception)
        {
            ServiceSessionUnavailable(logger, exception);

            return new ServiceSessionResponseModel
            {
                Success = false,
                Error = "Service session is unavailable.",
            };
        }
        catch (Exception exception)
        {
            ServiceSessionUnexpectedFailure(logger, exception);

            return new ServiceSessionResponseModel
            {
                Success = false,
                Error = "Unexpected failure while communicating with the service session.",
            };
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Service session request failed. Error: {Error}")]
    private static partial void ServiceSessionRequestFailed(
        ILogger logger,
        string error);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Service session is unavailable.")]
    private static partial void ServiceSessionUnavailable(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Unexpected failure while communicating with the service session.")]
    private static partial void ServiceSessionUnexpectedFailure(
        ILogger logger,
        Exception exception);
}
