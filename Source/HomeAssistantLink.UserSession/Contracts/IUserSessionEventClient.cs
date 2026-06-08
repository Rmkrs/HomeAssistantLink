namespace HomeAssistantLink.UserSession.Contracts;

public interface IUserSessionEventClient
{
    void PublishDisplaySnapshot(IReadOnlyList<DisplayObservationModel> displays);
}
