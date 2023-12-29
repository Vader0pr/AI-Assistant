using OpenAI.ObjectModels.RequestModels;
using System.Data;
using ProtoBuf;
using System.Runtime.InteropServices;
using AiAssistant;

namespace AI_Assistant.Sessions.Models
{
    [ProtoContract]
    internal sealed class Session()
    {
        [ProtoMember(1)] private readonly List<ChatMessageSerializable> _messages = [];
        internal void AddMessage(ChatMessage message)
        {
            _messages.Add(new ChatMessageSerializable(message.Role, message.Content, message.ToolCallId, message.ToolCalls));
        }
        public List<ChatMessage> Messages
        {
            get
            {
                List<ChatMessage> messages =
                [
                    ChatMessage.FromSystem(Settings.Load().SystemPrompt + "User's operating system: " + RuntimeInformation.OSDescription),
                    .. _messages.Select(x => x.ToChatMessage()),
                ];
                return messages;
            }
        }
    }
}