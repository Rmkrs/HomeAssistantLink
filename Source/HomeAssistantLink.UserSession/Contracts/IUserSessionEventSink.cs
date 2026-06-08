namespace HomeAssistantLink.UserSession.Contracts;

public interface IUserSessionEventSink
{
    Task HandleAsync(
        UserSessionEventRequestModel request,
        CancellationToken ct);
}
