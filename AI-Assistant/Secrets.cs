using Newtonsoft.Json;

namespace AiAssistant
{
    internal sealed class Secrets
    {
        private static Secrets? _secrets = null;
        private const string _secretsFileName = "secrets.json";
        private static readonly string _secretsFilePath = Path.Combine(new FileInfo(Environment.GetCommandLineArgs()[0]).DirectoryName ?? "", _secretsFileName);
        public string? OpenaiApiKey { get; set; } = null;
        public static async ValueTask<Secrets> LoadSecretsAsync()
        {
            if (_secrets != null) return _secrets;
            if (!File.Exists(_secretsFilePath)) return _secrets = new();
            return _secrets = JsonConvert.DeserializeObject<Secrets>(await File.ReadAllTextAsync(_secretsFilePath)) ?? new();
        }
        public async void Save() => await File.WriteAllTextAsync(_secretsFilePath, JsonConvert.SerializeObject(_secrets = this, Formatting.Indented));
    }
}