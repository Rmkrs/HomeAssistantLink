namespace HomeAssistantLink.Infrastructure;

using HomeAssistantLink.Domain.Contracts;

public class Debounce(TimeSpan? delay = null) : IDebounce
{
    private readonly Dictionary<string, object?> lastKnownValues = [];
    private readonly Dictionary<string, DateTime> lastUpdated = [];
    private readonly TimeSpan delay = delay ?? TimeSpan.FromSeconds(1);

    public bool ShouldProcess(string entityId, object? currentValue)
    {
        var now = DateTime.UtcNow;
        var lastValue = this.lastKnownValues.GetValueOrDefault(entityId);
        var lastTime = this.lastUpdated.GetValueOrDefault(entityId);

        if (Equals(currentValue, lastValue) && now - lastTime < this.delay)
        {
            return false;
        }

        this.lastKnownValues[entityId] = currentValue;
        this.lastUpdated[entityId] = now;
        return true;
    }
}
