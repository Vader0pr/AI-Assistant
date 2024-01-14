namespace AiAssistant.Exceptions
{
    public class AssistentNotInitializedException : Exception
    {
        public AssistentNotInitializedException() : base("Assistant not initialized.") { }
    }
}
