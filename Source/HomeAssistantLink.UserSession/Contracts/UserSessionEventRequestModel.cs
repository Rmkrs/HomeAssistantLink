namespace HomeAssistantLink.UserSession.Contracts;

public sealed class UserSessionEventRequestModel
{
    public UserSessionEventType EventType { get; set; }

    public List<DisplayObservationModel> Displays { get; set; } = [];
}
