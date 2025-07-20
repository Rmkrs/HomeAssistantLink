namespace HomeAssistantLink.Domain.Contracts;

public interface IPluginHandler
{
    void Handle(string entityId, string state);
}