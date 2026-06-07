namespace HomeAssistantLink.UserSession.Contracts;

public sealed class ServiceSessionRequestModel
{
    public ServiceSessionRequestType RequestType { get; set; }

    public string CommandId { get; set; } = string.Empty;
}
