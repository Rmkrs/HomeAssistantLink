namespace HomeAssistantLink.Plugins.ScriptRunner;

using System.Globalization;
using HomeAssistantLink.Domain;
using HomeAssistantLink.Domain.Contracts;
using HomeAssistantLink.Plugins.ScriptRunner.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed partial class ScriptRunnerPlugin(
    IOptions<ScriptRunnerPluginConfig> config,
    IScriptInvoker scriptInvoker,
    ILogger<ScriptRunnerPlugin> logger) : IPlugin
{
    public const string PluginType = "ScriptRunner";

    private readonly ScriptRunnerPluginConfig config = config.Value ?? throw new ArgumentNullException(nameof(config));

    public bool CanExecute(string entityId, string state)
    {
        return this.GetAction(entityId, state) != null;
    }

    public void Execute(string entityId, string state)
    {
        var action = this.GetAction(entityId, state);

        if (action is null)
        {
            IgnoringUnmatchedCommand(logger, entityId, state);
            return;
        }

        if (string.IsNullOrWhiteSpace(action.ScriptPath))
        {
            IgnoringActionWithoutScriptPath(logger, action.EntityId, action.Command);
            return;
        }

        scriptInvoker.Invoke(action);
    }

    public IEnumerable<PluginCommand> GetCommands()
    {
        return this.config.Actions
            .Where(action =>
                       !string.IsNullOrWhiteSpace(action.EntityId) &&
                       !string.IsNullOrWhiteSpace(action.Command))
            .Select(action => new PluginCommand(
                        PluginCommandId.Create(PluginType, action.EntityId, action.Command),
                        PluginType,
                        GetDisplayName(action),
                        action.EntityId,
                        action.Command,
                        action.RunAs,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["ScriptPath"] = action.ScriptPath,
                            ["TimeoutSeconds"] = action.TimeoutSeconds.ToString(CultureInfo.InvariantCulture),
                        }));
    }

    private static string GetDisplayName(ScriptRunnerActionConfig action)
    {
        return string.IsNullOrWhiteSpace(action.DisplayName)
            ? action.Command
            : action.DisplayName;
    }

    private ScriptRunnerActionConfig? GetAction(string entityId, string state)
    {
        return this.config.Actions.FirstOrDefault(
            configuredAction =>
                string.Equals(configuredAction.EntityId, entityId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(configuredAction.Command, state, StringComparison.OrdinalIgnoreCase));
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Ignoring script runner command. No configured action matched. EntityId: {EntityId}, State: {State}")]
    private static partial void IgnoringUnmatchedCommand(
        ILogger logger,
        string entityId,
        string state);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Ignoring script runner command because matched action has no script path. EntityId: {EntityId}, Command: {Command}")]
    private static partial void IgnoringActionWithoutScriptPath(
        ILogger logger,
        string entityId,
        string command);
}
