using OpenAI.ObjectModels.RequestModels;
using ProtoBuf;

namespace AiAssistant.Sessions.Models
{
    [ProtoContract]
    internal sealed class ChatMessageSerializable
    {
        [ProtoMember(1)] public string Role { get; set; } = "";
        [ProtoMember(2)] public string? Content { get; set; } = null;
        [ProtoMember(3)] public string? ToolCallId { get; set; } = null;
        [ProtoMember(4)] public IList<ToolCallSerializable>? ToolCalls { get; set; } = null;
        [ProtoMember(5)] public IList<MessageContentSerializable>? Contents { get; set; } = null;
        public ChatMessageSerializable() { }
        public ChatMessageSerializable(string role, string? content = null, IList<MessageContent>? contents = null, string? toolCallId = null, IList<ToolCall>? toolCalls = null)
        {
            Role = role;
            Content = content;
            ToolCallId = toolCallId;
            ToolCalls = toolCalls != null ? ToolCallSerializable.ToSerializableToolCalls(toolCalls) : null;
            Contents = contents != null ? MessageContentSerializable.ToSerializableMessageContent(contents) : null;
        }
        internal async Task<ChatMessage> ToChatMessage()
        {
            if (Contents != null)
            {
                List<MessageContent> contents = [];
                foreach(MessageContentSerializable? x in Contents) contents.Add(await x.ToMessageContent());
                return new(Role, contents, toolCallId: ToolCallId, toolCalls: ToolCalls != null ? ToolCallSerializable.ToToolCalls(ToolCalls) : null);
            }
            else return new(Role, Content ?? "", toolCallId: ToolCallId, toolCalls: ToolCalls != null ? ToolCallSerializable.ToToolCalls(ToolCalls) : null);
        }
    }
}