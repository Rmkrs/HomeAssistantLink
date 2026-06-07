namespace HomeAssistantLink.Domain.Contracts;

public sealed class PluginHostConfig
{
    public PluginRunAs RunAs { get; set; } = PluginRunAs.System;
}
