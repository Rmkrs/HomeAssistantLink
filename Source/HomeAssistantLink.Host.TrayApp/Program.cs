namespace HomeAssistantLink.Host.TrayApp;

using dotenv.net;

using HomeAssistantLink.Infrastructure;
using HomeAssistantLink.Plugins.ScriptRunner;
using HomeAssistantLink.UserSession;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class Program
{
    [STAThread]
    private static async Task Main(string[] args)
    {
        DotEnv.Fluent().WithProbeForEnv(int.MaxValue).Load();

        ApplicationConfiguration.Initialize();

        var builder = Host.CreateApplicationBuilder(args);

        builder
            .AddInfrastructure()
            .AddScriptRunner()
            .AddUserSessionServer()
            .AddServiceSessionClient();

        using var host = builder.Build();

        await host.StartAsync().ConfigureAwait(true);

        try
        {
            using var context = ActivatorUtilities.CreateInstance<TrayApplicationContext>(host.Services);

            Application.Run(context);
        }
        finally
        {
            await host.StopAsync().ConfigureAwait(true);
        }
    }
}
