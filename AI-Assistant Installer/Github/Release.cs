using Newtonsoft.Json;

namespace AiAssistant_Installer.Github
{
    public class Release
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("tag_name")]
        public string? TagName { get; set; }
        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }
        [JsonProperty("assets")]
        public ReleaseAsset[]? Assets { get; set; }
    }
}