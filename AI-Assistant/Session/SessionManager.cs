using AiAssistant.Sessions.Models;
using OpenAI.ObjectModels.RequestModels;
using ProtoBuf;

namespace AiAssistant.Sessions
{
    internal sealed class SessionManager
    {
        private const string _sessionFileName = ".session";
        private static readonly string _sesionFilePath = Path.Combine(new FileInfo(Environment.GetCommandLineArgs()[0]).DirectoryName ?? "", _sessionFileName);
        private Session? _session = null;
        public void ClearSession()
        {
            File.Delete(_sesionFilePath);
            _session = null;
        }
        public async ValueTask<IList<ChatMessage>> GetChatMessagesAsync()
        {
            if (_session != null) return await _session.GetMessages();
            else await LoadSession();
            return _session != null ? await _session.GetMessages() : [];
        }
        public async Task AddMessage(ChatMessage message)
        {
            if (_session == null) await LoadSession();
            (_session ??= new()).AddMessage(message);
            await using (FileStream fs = File.Create(_sesionFilePath))
            {
                Serializer.Serialize(fs, _session ?? new());
                await fs.FlushAsync();
            }
        }
        private async Task LoadSession()
        {
            if (!File.Exists(_sesionFilePath)) return;
            {
                await using (FileStream fs = File.OpenRead(_sesionFilePath))
                {
                    _session = Serializer.Deserialize<Session>(fs);
                    await fs.FlushAsync();
                }
            }
        }
    }
}