namespace HomeAssistantLink.Plugins.ShutDownComputer;

using System.Diagnostics;
using HomeAssistantLink.Plugins.ShutDownComputer.Contracts;

public class ShutdownInvoker : IShutdownInvoker
{
    public void Invoke()
    {
        Process.Start(
            new ProcessStartInfo("shutdown", "/s /t 0")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
            });
    }
}