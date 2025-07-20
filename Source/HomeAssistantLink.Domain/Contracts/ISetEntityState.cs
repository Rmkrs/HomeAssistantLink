namespace HomeAssistantLink.Domain.Contracts;

public interface ISetEntityState
{
    void SetBool(string entityId, bool value);

    void SetString(string entityId, string value);

    void SetNumber(string entityId, double value);

    void SetDate(string entityId, DateTime value);
}
