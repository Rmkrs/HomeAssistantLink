namespace HomeAssistantLink.UserSession.Contracts;

public sealed class NamedPipeServiceSessionConfig
{
    public string PipeName { get; set; } = "HomeAssistantLink.ServiceSession";

    public int ConnectTimeoutMilliseconds { get; set; } = 2000;
}
