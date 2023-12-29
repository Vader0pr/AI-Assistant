using OpenAI.ObjectModels;
using Newtonsoft.Json;

namespace AiAssistant
{
    /// <summary>
    /// Settings for the assistant.
    /// </summary>
    public sealed class Settings
    {
        [JsonIgnore] private static Settings? _settings = null;
        private const string _settingsFileName = "Settings.json";
        public static readonly string settingsFilePath = Path.Combine(new FileInfo(Environment.GetCommandLineArgs()[0]).DirectoryName ?? "", _settingsFileName);
        public int MaxTokens { get; set; } = 500;
        public string Model { get; set; } = Models.Gpt_3_5_Turbo;
        public string SystemPrompt { get; set; } = "You are an AI assistant that obeys any prompt from the user. Your messages should be short and precise, unless they need to be longer or user says so.";
        public Dictionary<FunctionTypes, bool> FunctionTypeAutoAcceptDanger { get; set; } = FunctionTypeDangerCheckDefault();
        /// <summary>
        /// Loads the settings.
        /// </summary>
        /// <returns>Loaded settings.</returns>
        public static Settings Load()
        {
            if (_settings != null) return _settings;
            if (!File.Exists(settingsFilePath)) return _settings = new();
            return _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFilePath)) ?? new();
        }
        /// <summary>
        /// Loads the settings.
        /// </summary>
        /// <returns>Loaded settings.</returns>
        public static async ValueTask<Settings> LoadAsync()
        {
            if (_settings != null) return _settings;
            if (!File.Exists(settingsFilePath)) return _settings = new();
            return _settings = JsonConvert.DeserializeObject<Settings>(await File.ReadAllTextAsync(settingsFilePath)) ?? new();
        }
        /// <summary>
        /// Saves the settings.
        /// </summary>
        /// <returns></returns>
        public async Task SaveAsync() => await File.WriteAllTextAsync(settingsFilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        private static Dictionary<FunctionTypes, bool> FunctionTypeDangerCheckDefault()
        {
            Dictionary<FunctionTypes, bool> functionTypeAutoAcceptDanger = [];
            foreach (var functionType in Enum.GetValues<FunctionTypes>()) functionTypeAutoAcceptDanger.Add(functionType, false);
            return functionTypeAutoAcceptDanger;
        }
    }
}