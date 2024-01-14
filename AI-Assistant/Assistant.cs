using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI;
using OpenAI.Utilities.FunctionCalling;
using AiAssistant.Functions;
using AiAssistant.Exceptions;
using AiAssistant.Sessions;
using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenAI.ObjectModels;
using static OpenAI.ObjectModels.Models;
using static System.Net.Mime.MediaTypeNames;
using AiAssistant.Enums;

namespace AiAssistant
{
    /// <summary>
    /// The main class to use the AI. You need to initialize it with <see cref="Initialize"/>.
    /// </summary>
    public static class Assistant
    {
        private const string _apiKeyVariableName = "OPENAI_API_KEY";
        private static bool _initialized = false;
        private static bool _enableDangerous = false;
        private static readonly SessionManager _sessionManager = new();
        private static AssistantEvents? _events = null;
        private static UiMode? _uiMode = null;
        public static bool IsInitialized => _initialized;
        public static bool DangerousEnabled => _enableDangerous;

        /// <summary>
        /// Initializes the assistant.
        /// </summary>
        /// <param name="enableDangerous">If set to true it will run any method marked as dangerous without a confirm prompt.</param>
        /// <param name="functionTypes">Array of functionTypes that the assistant can use.</param>
        public static void Initialize(UiMode uiMode, bool enableDangerous = false)
        {
            _enableDangerous = enableDangerous;
            _initialized = true;
            _uiMode = uiMode;
        }
        /// <summary>
        /// Initializes the assistant.
        /// </summary>
        /// <param name="functionTypes">Array of functionTypes that the assistant can use.</param>
        /// <param name="events">Events that occur when user is prompted.</param>
        public static void Initialize(UiMode uiMode, AssistantEvents events)
        {
            _enableDangerous = true;
            _events = events;
            _initialized = true;
            _uiMode = uiMode;
        }
        internal static async ValueTask<bool> DangerCheck(string name, FunctionTypes functionType)
        {
            if (!IsInitialized) throw new AssistentNotInitializedException();
            if ((await Settings.LoadAsync()).FunctionTypeAutoAcceptDanger.TryGetValue(functionType, out bool value) && value) return value;
            if (!DangerousEnabled) return false;
            if (_events?.DangerTask == null) return true;
            if (_events.DangerTask.Invoke(name)) return true;
            else return false;
        }
        /// <summary>
        /// Sends a request to the assistant.
        /// </summary>
        /// <param name="message">Message you want to send.</param>
        /// <returns>Response read from stream in parts.</returns>
        public static async IAsyncEnumerable<string> SendMessage(string message, string? image = null)
        {
            Secrets secrets = await Secrets.LoadSecretsAsync();
            if (!IsInitialized) throw new AssistentNotInitializedException();
            Settings settings = await Settings.LoadAsync();
            message = message.Replace("\"", "'");
            if (string.IsNullOrEmpty(secrets.OpenaiApiKey))
            {
                secrets.OpenaiApiKey = _events?.ApiKeyPrompt?.Invoke();
                if (string.IsNullOrEmpty(secrets.OpenaiApiKey)) { yield return "api key not set."; yield break; }
                else secrets.Save();
            }
            List<ToolDefinition>? tools = null;
            List<ChatMessage> messages = new(await _sessionManager.GetChatMessagesAsync());
            string model = SelectModel(image == null, ref messages, ref tools);
            OpenAIService service = new(new OpenAiOptions
            {
                ApiKey = secrets.OpenaiApiKey,
            });
            List<MessageContent>? messageContents = null;
            if (image != null && model == Models.Gpt_4_vision_preview)
            {
                MessageContent? messageContent = await ImagesInputToMessageContent(image);
                if (messageContent == null) { yield return "Bad image."; yield break; };
                messageContents =
                [
                    MessageContent.TextContent(message),
                    messageContent
                ];
                await _sessionManager.AddMessage(ChatMessage.FromUser(messageContents));
                messages.Add(ChatMessage.FromUser(messageContents));
            }
            else
            {
                await _sessionManager.AddMessage(ChatMessage.FromUser(message));
                messages.Add(ChatMessage.FromUser(message));
            }
            var request = service.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
            {
                Model = model,
                MaxTokens = settings.MaxTokens,
                Messages = messages,
                Tools = tools
            });
            ChatMessage? responseMessage = null;
            string toolCallId = "";
            string functionName = "";
            string functionArguments = "";
            string content = "";
            await foreach (var response in request)
            {
                if (!response.Successful) { yield return response.Error?.Message ?? ""; yield break; }
                responseMessage = response.Choices.First().Message;
                if (responseMessage.ToolCalls != null)
                {
                    var toolCall = responseMessage.ToolCalls.First();
                    if (string.IsNullOrEmpty(toolCallId)) toolCallId = toolCall.Id;
                    if (toolCall.FunctionCall != null)
                    {
                        functionName += toolCall.FunctionCall.Name ?? "";
                        functionArguments += toolCall.FunctionCall.Arguments ?? "";
                    }
                }
                else if (!string.IsNullOrEmpty(responseMessage.Content)) { yield return responseMessage.Content; content += responseMessage.Content; }
            }
            if (!string.IsNullOrEmpty(content)) await _sessionManager.AddMessage(ChatMessage.FromAssistant(content));
            if (string.IsNullOrEmpty(functionName) || string.IsNullOrEmpty(functionArguments) || string.IsNullOrEmpty(toolCallId)) yield break;
            FunctionCall functionCall = new() { Name = functionName, Arguments = functionArguments };
            if (functionCall != null)
            {
                await foreach (string output in ExecuteFunction(functionCall)) yield return output;
                if (toolCallId is not null)
                {
                    await _sessionManager.AddMessage(ChatMessage.FromAssistant("", toolCalls: new List<ToolCall>() { new() { Id = toolCallId, Type = "function", FunctionCall = new() { Name = functionName, Arguments = functionArguments } } }));
                    await _sessionManager.AddMessage(ChatMessage.FromTool("", toolCallId));
                }
                yield break;
            }
        }
        public static void ClearSession() => _sessionManager.ClearSession();
        internal static async IAsyncEnumerable<string> ExecuteFunction(FunctionCall call)
        {
            if (call == null) yield break;
            var args = call.ParseArguments().Values.AsEnumerable();
            if (call.Name == nameof(OperatingSystemInteractions.ExecuteCommand)) await foreach (string output in OperatingSystemInteractions.ExecuteCommand(args.First().ToString() ?? "")) yield return output;
            else if (call.Name == nameof(VideoDownloadInteractions.DownloadVideoOrAudioFromUrl)) await foreach (string output in VideoDownloadInteractions.DownloadVideoOrAudioFromUrl(args.Take(1).First().ToString() ?? "", bool.TryParse(args.Skip(1).Take(1).First().ToString(), out bool result) && result)) yield return output;
            else if (call.Name == nameof(VideoDownloadInteractions.DownloadVideoOrAudioFromName)) await foreach (string output in VideoDownloadInteractions.DownloadVideoOrAudioFromUrl(args.Take(1).First().ToString() ?? "", bool.TryParse(args.Skip(1).Take(1).First().ToString(), out bool result) && result)) yield return output;
        }
        /// <summary>
        /// Invokes the <see cref="AssistantEvents.ApiKeyPrompt"/> and sets the API key to its return value. If events are not set it doesn't do anything.
        /// </summary>
        public static async void UpdateApiKey()
        {
            Secrets secrets = await Secrets.LoadSecretsAsync();
            secrets.OpenaiApiKey = (_events != null && _events.ApiKeyPrompt != null) ? _events.ApiKeyPrompt.Invoke() : secrets.OpenaiApiKey;
            secrets.Save();
        }
        /// <summary>
        /// Sets the API key.
        /// </summary>
        /// <param name="apiKey">The new value for API key.</param>
        public static async Task UpdateApiKeyAsync(string apiKey)
        {
            Secrets secrets = await Secrets.LoadSecretsAsync();
            secrets.OpenaiApiKey = apiKey;
            secrets.Save();
        }
        /// <summary>
        /// Updates the program and all its dependencies using AI-Installer.exe.
        /// </summary>
        public static void UpdateProgram() => Process.Start(new ProcessStartInfo()
        {
            FileName = "AI-Installer.exe",
            UseShellExecute = true,
            WorkingDirectory = new FileInfo(Environment.CommandLine).Directory?.FullName
        });
        internal static async ValueTask<MessageContent?> ImagesInputToMessageContent(string image)
        {
            if (Regex.Match(image, @"https*:\/\/.+\..+").Success) return MessageContent.ImageUrlContent(image);
            else if (Regex.Match(image, @".+\.(png|webp|jpe*g|gif)").Success) return MessageContent.ImageBinaryContent(await File.ReadAllBytesAsync(image), image.Split('.').Last().ToUpperInvariant().Replace("JPG", "JPEG"));
            else if (Regex.Match(image, @"data:image.+").Success) return new() { Type = "image_url", ImageUrl = new() { Url = image } };
            return null;
        }
        private static FunctionTypes[] GetFunctionTypes() => _uiMode switch
        {
            UiMode.Console => [FunctionTypes.OperatingSystemInteractions, FunctionTypes.VideoDownloadInteractions],
            UiMode.Web => [FunctionTypes.VideoDownloadInteractions],
            UiMode.Application => [FunctionTypes.VideoDownloadInteractions],
            _ => []
        };
        private static string SelectModel(bool isImageNull, ref List<ChatMessage> messages, ref List<ToolDefinition>? tools)
        {
            FunctionTypes[] functionTypes = GetFunctionTypes();
            Settings settings = Settings.Load();
            string model = settings.Model;
            if (settings.AutoSelectModel && !isImageNull) model = Models.Gpt_4_vision_preview;
            else
            {
                if (settings.AutoSelectModel)
                {
#pragma warning disable CS8604
                    if (messages.Count > 0 && messages.AsQueryable().Where(x => x.Contents != null).Any(x => x.Contents.Any(x => x.Type != "image_url"))) model = Models.Gpt_4_vision_preview;
#pragma warning restore CS8604
                    else model = model switch
                    {
                        "gpt-4-vision-preview" or "gpt-4" => Models.Gpt_4,
                        _ => Models.Gpt_3_5_Turbo
                    };
                }
                if (model != Models.Gpt_4_vision_preview)
                {
                    tools = [];
                    if (functionTypes.Contains(FunctionTypes.OperatingSystemInteractions)) tools.AddRange(FunctionCallingHelper.GetToolDefinitions(typeof(OperatingSystemInteractions)));
                    if (functionTypes.Contains(FunctionTypes.VideoDownloadInteractions)) tools.AddRange(FunctionCallingHelper.GetToolDefinitions(typeof(VideoDownloadInteractions)));
                }
            }
            return model;
        }
    }
}