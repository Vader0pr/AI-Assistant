namespace AiAssistant_Installer
{
    public static class DownloadProgress
    {
        public static Dictionary<int, string> Progresses { get; set; } = new();
        public static void ReportProgress(int id, string progress)
        {
            Progresses[id] = progress;
        }
        public static void ClearProgress(int id) => Progresses.Remove(id);
        public static async void RefreshProgress()
        {
            Console.Clear();
            foreach (string progress in Progresses.Values) await Console.Out.WriteLineAsync(progress);
        }
    }
}