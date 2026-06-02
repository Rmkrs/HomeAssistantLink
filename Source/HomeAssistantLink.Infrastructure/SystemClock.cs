namespace HomeAssistantLink.Infrastructure;

using HomeAssistantLink.Domain.Contracts;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}