using Newtonsoft.Json;

namespace AiAssistant_Installer.Github
{
    public class GithubApiClient : IDisposable
    {
        private readonly HttpClient _client = new();
        public GithubApiClient()
        {
            _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("AI-Assistant-Installer", "0.1.0"));
            _client.DefaultRequestHeaders.Host = "api.github.com";
        }
        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) _client.Dispose();
            _disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~GithubApiClient() => Dispose(false);

        public async Task<Release?> GetLatestReleaseAsync(string owner, string repo)
        {
            string response = await _client.GetStringAsync($"https://api.github.com/repos/{owner}/{repo}/releases/latest");
            return JsonConvert.DeserializeObject<Release>(response);
        }
        public async Task<Release[]?> GetReleasesAsync(string owner, string repo, int releasesPerPage = 30, int page = 1)
        {
            string response = await _client.GetStringAsync($"https://api.github.com/repos/{owner}/{repo}/releases?per_page={releasesPerPage}&page={page}");
            return JsonConvert.DeserializeObject<Release[]>(response);
        }
    }
}