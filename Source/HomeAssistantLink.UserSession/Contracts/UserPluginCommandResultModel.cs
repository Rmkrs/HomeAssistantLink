namespace HomeAssistantLink.UserSession.Contracts;

public sealed class UserPluginCommandResultModel
{
    public bool Success { get; set; }

    public string? Error { get; set; }
}
