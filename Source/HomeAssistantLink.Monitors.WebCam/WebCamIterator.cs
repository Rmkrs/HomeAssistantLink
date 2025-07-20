namespace HomeAssistantLink.Monitors.WebCam;

using HomeAssistantLink.Monitors.WebCam.Contracts;
using Microsoft.Win32;

public class WebCamIterator : IWebCamIterator
{
    public IEnumerable<WebCamInfo> Iterate()
    {
        var webCamRoot = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam");
        return webCamRoot == null ? [] : GetItems(webCamRoot);
    }

    private static List<WebCamInfo> GetItems(RegistryKey webCamRoot)
    {
        var result = new List<WebCamInfo>();

        var items = webCamRoot.GetSubKeyNames();
        foreach (var item in items)
        {
            if (item == "NonPackaged")
            {
                var nonPackagedRoot = webCamRoot.OpenSubKey(item);
                if (nonPackagedRoot == null)
                {
                    continue;
                }

                result.AddRange(GetItems(nonPackagedRoot));
                continue;
            }

            var subKey = webCamRoot.OpenSubKey(item);

            if (subKey == null)
            {
                continue;
            }

            var start = subKey.GetValue("LastUsedTimeStart");

            if (start == null)
            {
                continue;
            }

            var stop = subKey.GetValue("LastUsedTimeStop");

            var lastUsedTimeStart = DateTime.FromFileTime((long)start);
            var lastUsedTimeStop = stop == null || (long)stop == 0 ? default(DateTime?) : DateTime.FromFileTime((long)stop);
            result.Add(new WebCamInfo { By = item, LastUsedTimeStart = lastUsedTimeStart, LastUsedTimeStop = lastUsedTimeStop });
        }

        return result;
    }
}