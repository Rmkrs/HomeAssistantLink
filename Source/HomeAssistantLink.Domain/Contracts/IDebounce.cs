namespace HomeAssistantLink.Domain.Contracts;

public interface IDebounce
{
    bool ShouldProcess(EntityStateUpdate update);
}