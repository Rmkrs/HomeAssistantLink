namespace HomeAssistantLink.UserSession.Contracts;

public sealed class NamedPipeUserSessionConfig
{
    public string PipeName { get; set; } = "HomeAssistantLink.UserSession";

    public int ConnectTimeoutMilliseconds { get; set; } = 2000;
}
