using Newtonsoft.Json;

namespace AiAssistant_Installer
{
    public class Versions
    {
        private const string _versionsFile = "Versions.json";
        [JsonProperty("ai-assistant")] public string? AiAssistant { get; set; } = "";
        [JsonProperty("ffmpeg")] public string? Ffmpeg { get; set; } = "";
        [JsonProperty("yt-dlp")] public string? Ytdlp { get; set; } = "";
        private static Versions? _instance = null;
        public static async ValueTask<Versions> Load()
        {
            if (_instance != null) return _instance;
            try { return _instance = JsonConvert.DeserializeObject<Versions>(await File.ReadAllTextAsync(_versionsFile)) ?? new Versions(); }
            catch (Exception) { return new Versions(); }
        }
        public async Task Save() => await File.WriteAllTextAsync(_versionsFile, JsonConvert.SerializeObject(this));
    }
}