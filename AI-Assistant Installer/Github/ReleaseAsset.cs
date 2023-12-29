using Newtonsoft.Json;

namespace AiAssistant_Installer.Github
{
    public class ReleaseAsset
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("content_type")]
        public string? ContentType { get; set; }
        [JsonProperty("size")]
        public int? Size { get; set; }
        [JsonProperty("browser_download_url")]
        public string? DownloadUrl { get; set; }
    }
}