namespace HomeAssistantLink.Infrastructure.Contracts;

public class DebounceInfo
{
    public required object CurrentValue { get; set; }

    public required object NewValue { get; set; }

    public DateTime LastUpdate { get; set; }
}