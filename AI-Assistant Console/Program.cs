using AiAssistant;
using System.Diagnostics;

namespace AiAssistant_Console
{
    internal sealed class Program
    {
        static async Task Main(string[] args)
        {
            Assistant.Initialize([FunctionTypes.OperatingSystemInteractions], new AssistantEvents()
            {
                DangerTask = DangerPrompt,
                ApiKeyPrompt = () =>
                {
                    Console.Write("Enter an OpenAI API key:\n>");
                    return Console.ReadLine() ?? "";
                }
            });
            if (args.Length == 0)
            {
                await Console.Out.WriteLineAsync("AI [message]     Sends a message.");
                await Console.Out.WriteLineAsync("AI --clear     Clears the session.");
                await Console.Out.WriteLineAsync("AI --settings     Opens the settings.");
                await Console.Out.WriteLineAsync("AI --apikey     Shows a prompt to set a new API key.");
                await Console.Out.WriteLineAsync("AI --update     Updates AI assistant and all dependencies.");
                return;
            }
            switch (args[0].ToLower())
            {
                case "--clear": Assistant.ClearSession(); break;
                case "--settings":
                    var process = new Process() { StartInfo = new ProcessStartInfo { FileName = Settings.settingsFilePath, UseShellExecute = true } };
                    if (File.Exists(Settings.settingsFilePath)) process.Start();
                    else
                    {
                        await new Settings().SaveAsync();
                        process.Start();
                    }
                    break;
                case "--apikey": Assistant.UpdateApiKey(); break;
                case "--update": Assistant.UpdateProgram(); break;
                default:
                    string message = args[0];
                    for (int i = 1; i < args.Length; i++) message += " " + args[i];
                    await foreach (string output in Assistant.SendRequestAsync(message)) await Console.Out.WriteAsync(output);
                    break;
            }
        }
        public static bool DangerPrompt(string name)
        {
            Console.WriteLine($"Confirm: '{name}'. Type 'y' to confirm or 'n' to cancel.");
            while (true)
            {
                Console.Write(">");
                string? input = Console.ReadLine();
                if (input == null) continue;
                if (input == "y") return true;
                if (input == "n") return false;
            }
        }
    }
}