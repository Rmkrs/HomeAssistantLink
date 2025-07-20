namespace HomeAssistantLink.Monitors.WebCam.Contracts;

public interface IWebCamIterator
{
    IEnumerable<WebCamInfo> Iterate();
}