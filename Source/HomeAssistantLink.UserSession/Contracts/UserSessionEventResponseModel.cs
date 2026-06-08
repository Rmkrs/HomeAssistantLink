namespace HomeAssistantLink.UserSession.Contracts;

public sealed class UserSessionEventResponseModel
{
    public bool Success { get; set; }

    public string? Error { get; set; }
}
