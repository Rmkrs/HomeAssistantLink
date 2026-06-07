namespace HomeAssistantLink.UserSession.Contracts;

public interface IServiceSessionClient
{
    ServiceSessionCommandListResult GetUserCommands();

    void Execute(string commandId);
}
