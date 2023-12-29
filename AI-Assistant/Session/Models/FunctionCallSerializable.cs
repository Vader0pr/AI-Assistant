using OpenAI.ObjectModels.RequestModels;
using ProtoBuf;

namespace AI_Assistant.Sessions.Models
{
    [ProtoContract]
    internal sealed class FunctionCallSerializable
    {
        [ProtoMember(1)] public string? Name { get; set; }
        [ProtoMember(2)] public string? Arguments { get; set; }
        public FunctionCallSerializable() { }
        public FunctionCallSerializable(string? name, string? arguments)
        {
            Name = name;
            Arguments = arguments;
        }
        public FunctionCall ToFunctionCall() => new() { Name = Name, Arguments = Arguments };
    }
}