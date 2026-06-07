namespace HomeAssistantLink.Domain.Contracts;

public interface IUserPluginCommandClient
{
    void Handle(PluginCommand command);
}
