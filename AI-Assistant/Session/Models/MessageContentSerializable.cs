using OpenAI.ObjectModels.RequestModels;
using ProtoBuf;

namespace AiAssistant.Sessions.Models
{
    [ProtoContract] internal sealed class MessageContentSerializable
    {
        [ProtoMember(1)] public string Type { get; set; } = "text";
        [ProtoMember(2)] public string? Text { get; set; }
        [ProtoMember(3)] public string? Url { get; set; }
        public MessageContentSerializable() { }
        public MessageContentSerializable(string type, string? text = null, string? url = null)
        {
            Type = type;
            Text = text;
            Url = url;
        }
        internal async ValueTask<MessageContent> ToMessageContent()
        {
            if (Text != null) return MessageContent.TextContent(Text);
            else return await Assistant.ImagesInputToMessageContent(Url ?? "") ?? MessageContent.TextContent("");
        }
        public static IList<MessageContentSerializable> ToSerializableMessageContent(IList<MessageContent> messageConents)
        {
            List<MessageContentSerializable> serializableMessageContent = [];
            foreach (MessageContent content in messageConents) serializableMessageContent.Add(new(content.Type, content.Text, content.ImageUrl?.Url));
            return serializableMessageContent;
        }
    }
}