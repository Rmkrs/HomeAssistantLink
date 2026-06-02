namespace HomeAssistantLink.Domain.Contracts;

public interface ISetEntityState
{
    Task SetAsync(EntityStateUpdate update, CancellationToken cancellationToken);
}