namespace HomeAssistantLink.Domain.Contracts;

public interface IDebounce
{
    bool ShouldProcess(string entityId, object? currentValue);
}
