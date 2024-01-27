using AiAssistant.Enums;
using OpenAI.Utilities.FunctionCalling;
using System.Diagnostics;

namespace AiAssistant.Functions
{
    internal static class OperatingSystemInteractions
    {
        [FunctionDescription("Executes a command using command line.")] public static async IAsyncEnumerable<string> ExecuteCommand(string command)
        {
            if (!await Assistant.DangerCheck(nameof(ExecuteCommand) + $"(\"{command}\")", FunctionTypes.OperatingSystemInteractions)) { yield return "Dangerous command canceled"; yield break; }
            Settings settings = await Settings.LoadAsync();
            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = settings.GetPlatform() switch
                {
                    Platforms.Linux => "/bin/bash",
                    Platforms.Windows => "cmd",
                    _ => "/bin/bash"
                },
                Arguments = settings.GetPlatform() switch
                {
                    Platforms.Windows => $"/c \"{command}\"",
                    Platforms.Linux => $"-c \"{command}\"",
                    _ => "-c \" " + command + " \""
                },
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardOutput = true
            });
            if (process == null) { yield return "Failed to execute a command"; yield break; }
            while (!process.StandardOutput.EndOfStream)
            {
                char[] buffer = new char[1024];
                await process.StandardOutput.ReadAsync(buffer.AsMemory());
                foreach (char c in buffer) yield return c.ToString();
            }
        }
    }
}