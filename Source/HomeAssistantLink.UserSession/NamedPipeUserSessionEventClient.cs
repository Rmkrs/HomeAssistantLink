namespace HomeAssistantLink.UserSession;

using System.IO.Pipes;
using System.Text.Json;

using HomeAssistantLink.UserSession.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed partial class NamedPipeUserSessionEventClient(
    IOptions<NamedPipeUserSessionEventConfig> options,
    ILogger<NamedPipeUserSessionEventClient> logger) : IUserSessionEventClient
{
    private readonly NamedPipeUserSessionEventConfig options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public void PublishDisplaySnapshot(IReadOnlyList<DisplayObservationModel> displays)
    {
        ArgumentNullException.ThrowIfNull(displays);

        var request = new UserSessionEventRequestModel
        {
            EventType = UserSessionEventType.DisplaySnapshot,
            Displays = [.. displays],
        };

        this.Send(request);
    }

    private void Send(UserSessionEventRequestModel request)
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
                UserSessionEventEmptyResponse(logger);
                return;
            }

            var response = JsonSerializer.Deserialize<UserSessionEventResponseModel>(responsePayload);

            if (response?.Success == true)
            {
                UserSessionEventPublished(logger, request.EventType);
                return;
            }

            UserSessionEventFailed(logger, response?.Error ?? "Unknown error.");
        }
        catch (TimeoutException exception)
        {
            UserSessionEventPipeUnavailable(logger, exception);
        }
        catch (IOException exception)
        {
            UserSessionEventPipeUnavailable(logger, exception);
        }
        catch (Exception exception)
        {
            UserSessionEventUnexpectedFailure(logger, exception);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "User session event pipe returned an empty response.")]
    private static partial void UserSessionEventEmptyResponse(
        ILogger logger);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "User session event published. EventType: {EventType}")]
    private static partial void UserSessionEventPublished(
        ILogger logger,
        UserSessionEventType eventType);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "User session event failed. Error: {Error}")]
    private static partial void UserSessionEventFailed(
        ILogger logger,
        string error);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "User session event pipe is unavailable.")]
    private static partial void UserSessionEventPipeUnavailable(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Unexpected failure while publishing user session event.")]
    private static partial void UserSessionEventUnexpectedFailure(
        ILogger logger,
        Exception exception);
}
