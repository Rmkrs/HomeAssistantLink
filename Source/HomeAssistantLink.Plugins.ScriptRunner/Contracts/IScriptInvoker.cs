namespace HomeAssistantLink.Plugins.ScriptRunner.Contracts;

public interface IScriptInvoker
{
    void Invoke(ScriptRunnerActionConfig action);
}
