namespace AiAssistant
{
    public sealed class AssistantEvents
    {
        /// <summary>
        /// Prompt to user in case of AI executing a potentially dangerous task.
        /// </summary>
        public Func<string, bool>? DangerTask { get; set; } = null;
        /// <summary>
        /// Prompt to user when API key is not set.
        /// </summary>
        public Func<string>? ApiKeyPrompt { get; set; } = null;
    }
}