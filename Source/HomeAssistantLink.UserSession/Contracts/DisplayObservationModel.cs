namespace HomeAssistantLink.UserSession.Contracts;

public sealed class DisplayObservationModel
{
    public string DeviceName { get; set; } = string.Empty;

    public bool IsConnected { get; set; }
}
