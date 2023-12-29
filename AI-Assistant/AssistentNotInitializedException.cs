namespace AiAssistant.Exceptions
{
    public class AssistentNotInitializedException : Exception
    {
        public AssistentNotInitializedException() : base("Assistent not initialized.") { }
    }
}
