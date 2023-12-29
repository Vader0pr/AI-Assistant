using OpenAI.ObjectModels.RequestModels;
using ProtoBuf;

namespace AI_Assistant.Sessions.Models
{
    [ProtoContract]
    internal sealed class ToolCallSerializable
    {
        [ProtoMember(1)] public string Id { get; set; } = "";
        [ProtoMember(2)] public string Type { get; set; } = "";
        [ProtoMember(3)] public FunctionCallSerializable? FunctionCall { get; set; }
        public ToolCallSerializable() { }
        public ToolCallSerializable(string id, string type, FunctionCallSerializable functionCall)
        {
            Id = id;
            Type = type;
            FunctionCall = functionCall;
        }
        public static IList<ToolCall> ToToolCalls(IList<ToolCallSerializable> serializalbeToolCalls)
        {
            List<ToolCall> toolCalls = [];
            foreach (ToolCallSerializable call in serializalbeToolCalls) toolCalls.Add(new()
            {
                Id = call.Id,
                Type = call.Type,
                FunctionCall = call.FunctionCall.ToFunctionCall()
            });
            return toolCalls;
        }
        public static IList<ToolCallSerializable> ToSerializableToolCalls(IList<ToolCall> toolCalls)
        {
            List<ToolCallSerializable> serializableToolCalls = [];
            foreach (ToolCall call in toolCalls) serializableToolCalls.Add(new(call.Id, call.Type, new(call.FunctionCall?.Name, call.FunctionCall?.Arguments)));
            return serializableToolCalls;
        }
    }
}