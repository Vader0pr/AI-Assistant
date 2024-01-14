using OpenAI.ObjectModels.RequestModels;
using ProtoBuf;
using System.Runtime.InteropServices;

namespace AiAssistant.Sessions.Models
{
    [ProtoContract]
    internal sealed class Session()
    {
        [ProtoMember(1)] private readonly List<ChatMessageSerializable> _messages = [];
        internal void AddMessage(ChatMessage message) => _messages.Add(new ChatMessageSerializable(message.Role, message.Content, message.Contents, message.ToolCallId, message.ToolCalls));
        public async Task<IList<ChatMessage>> GetMessages()
        {
            List<ChatMessage> messages = [ChatMessage.FromSystem((await Settings.LoadAsync()).SystemPrompt + " User's operating system: " + RuntimeInformation.OSDescription)];
            foreach (ChatMessageSerializable message in _messages) messages.Add(await message.ToChatMessage());
            return messages;
        }
    }
}