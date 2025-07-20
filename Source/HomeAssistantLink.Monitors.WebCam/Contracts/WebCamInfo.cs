namespace HomeAssistantLink.Monitors.WebCam.Contracts;

public class WebCamInfo
{
    public required string By { get; set; }

    public DateTime? LastUsedTimeStart { get; set; }

    public DateTime? LastUsedTimeStop { get; set; }

    public bool IsInUse => this.LastUsedTimeStart.HasValue && !this.LastUsedTimeStop.HasValue;
}