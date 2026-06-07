namespace HomeAssistantLink.Plugins.ScriptRunner;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Plugins.ScriptRunner.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddScriptRunner(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IPlugin, ScriptRunnerPlugin>();
        builder.Services.AddSingleton<IScriptInvoker, ScriptInvoker>();
        builder.Services.AddSingleton<IUserPluginCommandExecutor, ScriptRunnerUserPluginCommandExecutor>();

        builder.Services.Configure<ScriptRunnerPluginConfig>(
            builder.Configuration.GetSection("HomeAssistantLink:Plugins:ScriptRunnerPlugin"));

        return builder;
    }
}
