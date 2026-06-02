namespace HomeAssistantLink.Domain.Contracts;

public interface IClock
{
    DateTime UtcNow { get; }
}