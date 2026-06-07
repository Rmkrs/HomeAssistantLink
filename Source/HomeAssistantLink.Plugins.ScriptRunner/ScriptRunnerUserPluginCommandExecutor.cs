namespace HomeAssistantLink.Plugins.ScriptRunner;

using System.Globalization;

using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Plugins.ScriptRunner.Contracts;

public sealed class ScriptRunnerUserPluginCommandExecutor(
    IScriptInvoker scriptInvoker) : IUserPluginCommandExecutor
{
    public string PluginType => ScriptRunnerPlugin.PluginType;

    public void Execute(PluginCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.Parameters.TryGetValue("ScriptPath", out var scriptPath))
        {
            throw new InvalidOperationException("Script runner command is missing ScriptPath.");
        }

        var timeoutSeconds = 30;

        if (command.Parameters.TryGetValue("TimeoutSeconds", out var configuredTimeoutSeconds))
        {
            timeoutSeconds = int.Parse(configuredTimeoutSeconds, CultureInfo.InvariantCulture);
        }

        scriptInvoker.Invoke(new ScriptRunnerActionConfig
        {
            RunAs = command.RunAs,
            DisplayName = command.DisplayName,
            EntityId = command.EntityId,
            Command = command.State,
            ScriptPath = scriptPath,
            TimeoutSeconds = timeoutSeconds,
        });
    }
}
