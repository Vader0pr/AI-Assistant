using AiAssistant;
using OpenAI.ObjectModels;
using System.Diagnostics;

namespace AiAssistant_Console
{
    internal sealed class Program
    {
        static async Task Main(string[] args)
        {
            Assistant.Initialize(UiMode.Console, new AssistantEvents()
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
                await Console.Out.WriteLineAsync($"AI -i [image] [message]     Sends a message. Optionally you can attach an image url or file path(images work only with: {Models.Gpt_4_vision_preview}. Read this to see how to get access to GPT-4: https://help.openai.com/en/articles/7102672-how-can-i-access-gpt-4).");
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
                    await Console.Out.WriteLineAsync($"Recommended models:\n{Models.Gpt_3_5_Turbo}\n{Models.Gpt_4}\n{Models.Gpt_4_vision_preview}");
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
                    string? image = null;
                    int i = 0;
                    if (args[i] == "-i" && args.Length > 1) image = args[++i];
                    string message = args[i++];
                    while (i < args.Length) message += " " + args[i++];
                    Console.ForegroundColor = ConsoleColor.Blue;
                    try
                    {
                        await foreach (string output in Assistant.SendMessage(message, image)) await Console.Out.WriteAsync(output);
                    }
                    catch (Exception ex)
                    {
                        await Console.Out.WriteLineAsync(ex.Message);
                    }
                    finally
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
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