namespace HomeAssistantLink.Plugins.ScriptRunner.Contracts;

public sealed class ScriptRunnerPluginConfig
{
    public List<ScriptRunnerActionConfig> Actions { get; set; } = [];
}
