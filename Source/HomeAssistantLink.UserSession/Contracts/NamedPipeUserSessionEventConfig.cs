namespace HomeAssistantLink.UserSession.Contracts;

public sealed class NamedPipeUserSessionEventConfig
{
    public string PipeName { get; set; } = "HomeAssistantLink.UserSessionEvents";

    public int ConnectTimeoutMilliseconds { get; set; } = 2000;
}
