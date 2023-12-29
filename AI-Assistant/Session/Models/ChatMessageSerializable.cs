using OpenAI.ObjectModels.RequestModels;
using ProtoBuf;

namespace AI_Assistant.Sessions.Models
{
    [ProtoContract]
    internal sealed class ChatMessageSerializable
    {
        [ProtoMember(1)] public string Role { get; set; } = "";
        [ProtoMember(2)] public string? Content { get; set; } = null;
        [ProtoMember(3)] public string? ToolCallId { get; set; } = null;
        [ProtoMember(4)] public IList<ToolCallSerializable>? ToolCalls { get; set; } = null;
        public ChatMessageSerializable() { }
        public ChatMessageSerializable(string role, string? content = null, string? toolCallId = null, IList<ToolCall>? toolCalls = null)
        {
            Role = role;
            Content = content;
            ToolCallId = toolCallId;
            ToolCalls = toolCalls != null ? ToolCallSerializable.ToSerializableToolCalls(toolCalls) : null;
        }
        internal ChatMessage ToChatMessage() => new(Role, Content, toolCallId: ToolCallId, toolCalls: ToolCalls != null ? ToolCallSerializable.ToToolCalls(ToolCalls) : null);
    }
}