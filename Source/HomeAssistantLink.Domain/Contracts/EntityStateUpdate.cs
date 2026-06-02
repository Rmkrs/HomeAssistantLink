namespace HomeAssistantLink.Domain.Contracts;

public sealed record EntityStateUpdate(
    string EntityId,
    HomeAssistantEntityType EntityType,
    object? Value);