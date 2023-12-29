using AI_Assistant.Sessions.Models;
using OpenAI.ObjectModels.RequestModels;
using ProtoBuf;

namespace AI_Assistant.Sessions
{
    internal sealed class SessionManager
    {
        private const string _sessionFileName = "Session.sm";
        private static readonly string _sesionFilePath = Path.Combine(new FileInfo(Environment.GetCommandLineArgs()[0]).DirectoryName ?? "", _sessionFileName);
        private Session? _session = null;
        public void ClearSession()
        {
            File.Delete(_sesionFilePath);
            _session = null;
        }
        public async ValueTask<List<ChatMessage>> GetChatMessagesAsync()
        {
            if (_session != null) return _session.Messages;
            else await LoadSession();
            return _session != null ? _session.Messages : [];
        }
        public async Task AddMessage(ChatMessage message)
        {
            if (_session == null) await LoadSession();
            (_session ??= new()).AddMessage(message);
            using (FileStream fs = File.Create(_sesionFilePath))
            {
                Serializer.Serialize(fs, _session ?? new());
                await fs.FlushAsync();
                fs.Close();
                await fs.DisposeAsync();
            }
        }
        private async Task LoadSession()
        {
            if (!File.Exists(_sesionFilePath)) return;
            {
                using (FileStream fs = File.OpenRead(_sesionFilePath))
                {
                    _session = Serializer.Deserialize<Session>(fs);
                    await fs.FlushAsync();
                    fs.Close();
                    await fs.DisposeAsync();
                }
            }
        }
    }
}