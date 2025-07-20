namespace HomeAssistantLink.Host.WebApi.Models
{
    public class StateModel
    {
        public required string EntityId { get; set; }

        public required string State { get; set; }
    }
}
