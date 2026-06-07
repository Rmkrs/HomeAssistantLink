namespace HomeAssistantLink.Plugins.ScriptRunner;

using System.Diagnostics;
using HomeAssistantLink.Plugins.ScriptRunner.Contracts;
using Microsoft.Extensions.Logging;

internal sealed partial class ScriptInvoker(ILogger<ScriptInvoker> logger) : IScriptInvoker
{
    public void Invoke(ScriptRunnerActionConfig action)
    {
        var scriptPath = Path.GetFullPath(action.ScriptPath);

        if (!File.Exists(scriptPath))
        {
            ConfiguredScriptDoesNotExist(logger, scriptPath);
            return;
        }

        if (!string.Equals(Path.GetExtension(scriptPath), ".ps1", StringComparison.OrdinalIgnoreCase))
        {
            ConfiguredScriptIsNotPowerShellScript(logger, scriptPath);
            return;
        }

        var timeoutSeconds = Math.Max(1, action.TimeoutSeconds);
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        StartingConfiguredScript(
            logger,
            action.EntityId,
            action.Command,
            scriptPath);

        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Environment.CurrentDirectory,
        };

        process.StartInfo.ArgumentList.Add("-NoLogo");
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-NonInteractive");
        process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
        process.StartInfo.ArgumentList.Add("Bypass");
        process.StartInfo.ArgumentList.Add("-File");
        process.StartInfo.ArgumentList.Add(scriptPath);

        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(timeout))
            {
                ConfiguredScriptTimedOut(
                    logger,
                    timeoutSeconds,
                    scriptPath);

                process.Kill(entireProcessTree: true);
                process.WaitForExit();

                return;
            }

            var output = outputTask.GetAwaiter().GetResult();
            var error = errorTask.GetAwaiter().GetResult();

            if (logger.IsEnabled(LogLevel.Information) &&
                !string.IsNullOrWhiteSpace(output))
            {
                var trimmedOutput = output.Trim();

                ConfiguredScriptOutput(
                    logger,
                    scriptPath,
                    trimmedOutput);
            }

            if (logger.IsEnabled(LogLevel.Warning) &&
                !string.IsNullOrWhiteSpace(error))
            {
                var trimmedError = error.Trim();

                ConfiguredScriptErrorOutput(
                    logger,
                    scriptPath,
                    trimmedError);
            }

            if (process.ExitCode == 0)
            {
                ConfiguredScriptCompletedSuccessfully(logger, scriptPath);
                return;
            }

            ConfiguredScriptExitedWithCode(
                logger,
                process.ExitCode,
                scriptPath);
        }
        catch (Exception exception)
        {
            FailedToExecuteConfiguredScript(logger, exception, scriptPath);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Configured script does not exist. ScriptPath: {ScriptPath}")]
    private static partial void ConfiguredScriptDoesNotExist(
        ILogger logger,
        string scriptPath);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Configured script is not a PowerShell script. ScriptPath: {ScriptPath}")]
    private static partial void ConfiguredScriptIsNotPowerShellScript(
        ILogger logger,
        string scriptPath);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Starting configured script. EntityId: {EntityId}, Command: {Command}, ScriptPath: {ScriptPath}")]
    private static partial void StartingConfiguredScript(
        ILogger logger,
        string entityId,
        string command,
        string scriptPath);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Configured script timed out after {TimeoutSeconds} seconds. Killing process. ScriptPath: {ScriptPath}")]
    private static partial void ConfiguredScriptTimedOut(
        ILogger logger,
        int timeoutSeconds,
        string scriptPath);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Configured script output. ScriptPath: {ScriptPath}, Output: {Output}")]
    private static partial void ConfiguredScriptOutput(
        ILogger logger,
        string scriptPath,
        string output);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Configured script error output. ScriptPath: {ScriptPath}, Error: {Error}")]
    private static partial void ConfiguredScriptErrorOutput(
        ILogger logger,
        string scriptPath,
        string error);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Configured script completed successfully. ScriptPath: {ScriptPath}")]
    private static partial void ConfiguredScriptCompletedSuccessfully(
        ILogger logger,
        string scriptPath);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "Configured script exited with code {ExitCode}. ScriptPath: {ScriptPath}")]
    private static partial void ConfiguredScriptExitedWithCode(
        ILogger logger,
        int exitCode,
        string scriptPath);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message = "Failed to execute configured script. ScriptPath: {ScriptPath}")]
    private static partial void FailedToExecuteConfiguredScript(
        ILogger logger,
        Exception exception,
        string scriptPath);
}