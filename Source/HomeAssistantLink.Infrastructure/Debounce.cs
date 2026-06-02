namespace HomeAssistantLink.Infrastructure;

using HomeAssistantLink.Domain.Contracts;

public class Debounce(IClock clock, TimeSpan? delay = null) : IDebounce
{
    private readonly Lock lockObject = new();
    private readonly IClock clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly Dictionary<string, object?> lastKnownValues = [];
    private readonly Dictionary<string, DateTime> lastUpdated = [];
    private readonly TimeSpan delay = delay ?? TimeSpan.FromSeconds(1);

    public bool ShouldProcess(EntityStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);

        lock (this.lockObject)
        {
            var now = this.clock.UtcNow;
            var lastValue = this.lastKnownValues.GetValueOrDefault(update.EntityId);
            var lastTime = this.lastUpdated.GetValueOrDefault(update.EntityId);

            if (object.Equals(update.Value, lastValue) && now - lastTime < this.delay)
            {
                return false;
            }

            this.lastKnownValues[update.EntityId] = update.Value;
            this.lastUpdated[update.EntityId] = now;

            return true;
        }
    }
}