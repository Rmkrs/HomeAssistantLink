namespace HomeAssistantLink.Domain.Contracts;

public interface IPlugin
{
    void Execute(string entityId, string state);
}