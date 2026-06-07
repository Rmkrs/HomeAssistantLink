namespace HomeAssistantLink.Plugins.ScriptRunner.Contracts;

using HomeAssistantLink.Domain.Contracts;

public sealed class ScriptRunnerActionConfig
{
    public PluginRunAs RunAs { get; set; } = PluginRunAs.System;

    public string DisplayName { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;

    public string ScriptPath { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
}
