using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI;
using OpenAI.Utilities.FunctionCalling;
using AiAssistant.Functions;
using AiAssistant.Exceptions;
using AI_Assistant.Sessions;
using System.Diagnostics;

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
        private static string? _apiKey = null;
        private static FunctionTypes[]? _functionTypes = null;
        private static string? ApiKey
        {
            get
            {
                if (_apiKey != null) return _apiKey;
                string? content = Environment.GetEnvironmentVariable(_apiKeyVariableName, EnvironmentVariableTarget.User);
                if (content != null && content != "") _apiKey = content;
                else if (_events?.ApiKeyPrompt != null) Environment.SetEnvironmentVariable(_apiKeyVariableName, (_apiKey = _events.ApiKeyPrompt.Invoke()), EnvironmentVariableTarget.User);
                return _apiKey;
            }
            set => _apiKey = value;
        }
        public static bool IsInitialized => _initialized;
        public static bool DangerousEnabled => _enableDangerous;

        /// <summary>
        /// Initializes the assistant.
        /// </summary>
        /// <param name="enableDangerous">If set to true it will run any method marked as dangerous without a confirm prompt.</param>
        /// <param name="functionTypes">Array of functionTypes that the assistant can use.</param>
        public static void Initialize(FunctionTypes[] functionTypes, bool enableDangerous = false)
        {
            _enableDangerous = enableDangerous;
            _functionTypes = functionTypes;
            _initialized = true;
        }
        /// <summary>
        /// Initializes the assistant.
        /// </summary>
        /// <param name="functionTypes">Array of functionTypes that the assistant can use.</param>
        /// <param name="events">Events that occur when user is prompted.</param>
        public static void Initialize(FunctionTypes[] functionTypes, AssistantEvents events)
        {
            _enableDangerous = true;
            _events = events;
            _functionTypes = functionTypes;
            _initialized = true;
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
        public static async IAsyncEnumerable<string> SendRequestAsync(string message)
        {
            if (!IsInitialized) throw new AssistentNotInitializedException();
            Settings settings = await Settings.LoadAsync();
            message = message.Replace("\"", "'");
            if (ApiKey == null || ApiKey == "") { yield return "api key not set."; yield break; }
            if (_functionTypes == null) { yield return "No function types selected."; yield break; }
            List<ToolDefinition> tools = [];
            if (_functionTypes.Contains(FunctionTypes.OperatingSystemInteractions))
                tools.AddRange(FunctionCallingHelper.GetToolDefinitions(typeof(OperatingSystemIneraction)));
            OpenAIService service = new(new OpenAiOptions
            {
                ApiKey = ApiKey,
            });
            await _sessionManager.AddMessage(ChatMessage.FromUser(message));
            List<ChatMessage> messages = await _sessionManager.GetChatMessagesAsync();
            var request = service.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
            {
                Model = settings.Model,
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
                    await _sessionManager.AddMessage(ChatMessage.FromAssistant(null, toolCalls: new List<ToolCall>() { new() { Id = toolCallId, Type = "function", FunctionCall = new() { Name = functionName, Arguments = functionArguments } } }));
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
            if (call.Name == nameof(OperatingSystemIneraction.ExecuteCommand)) await foreach (string output in OperatingSystemIneraction.ExecuteCommand(args.First().ToString() ?? "")) yield return output;
            else if (call.Name == nameof(OperatingSystemIneraction.DownloadVideoOrAudioFromUrl)) await foreach (string output in OperatingSystemIneraction.DownloadVideoOrAudioFromUrl(args.Take(1).First().ToString() ?? "", bool.Parse(args.Take(1).First().ToString()))) yield return output;
            else if (call.Name == nameof(OperatingSystemIneraction.DownloadVideoOrAudioFromName)) await foreach (string output in OperatingSystemIneraction.DownloadVideoOrAudioFromName(args.Take(1).First().ToString() ?? "", bool.Parse(args.Skip(1).Take(1).First().ToString()))) yield return output;
        }
        /// <summary>
        /// Invokes the <see cref="AssistantEvents.ApiKeyPrompt"/> and sets the API key to its return value. If events are not set it doesn't do anything.
        /// </summary>
        public static void UpdateApiKey() => ApiKey = (_events != null && _events.ApiKeyPrompt != null) ? _events.ApiKeyPrompt.Invoke() : ApiKey;
        /// <summary>
        /// Sets the API key.
        /// </summary>
        /// <param name="apiKey">The new value for API key.</param>
        public static void UpdateApiKey(string apiKey) => ApiKey = apiKey;
        public static void UpdateProgram() => Process.Start(new ProcessStartInfo()
        {
            FileName = "AI-Installer.exe",
            UseShellExecute = true,
            WorkingDirectory = new FileInfo(Environment.CommandLine).Directory?.FullName
        });
    }
}