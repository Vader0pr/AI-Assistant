using AiAssistant.Enums;
using OpenAI.Utilities.FunctionCalling;
using System.Diagnostics;

namespace AiAssistant.Functions
{
    internal static class VideoDownloadInteractions
    {
        [FunctionDescription("Downloads a video or audio by name")]
        public static async IAsyncEnumerable<string> DownloadVideoOrAudioFromName(string name, [ParameterDescription("Option to download only audio when downloading a video")] bool onlyAudio)
        {
            if (!await Assistant.DangerCheck(nameof(DownloadVideoOrAudioFromName) + $"(\"{name}\", {onlyAudio})", FunctionTypes.VideoDownloadInteractions)) { yield return "Dangerous command canceled"; yield break; }
            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"\"ytsearch:{name}\"" + (onlyAudio ? " -x" : ""),
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardOutput = true
            });
            if (process == null) { yield return "Failed download a video"; yield break; }
            while (!process.StandardOutput.EndOfStream)
            {
                char[] buffer = new char[1024];
                await process.StandardOutput.ReadAsync(buffer.AsMemory());
                foreach (char c in buffer) yield return c.ToString();
            }
        }
        [FunctionDescription("Downloads a video or audio from a url")]
        public static async IAsyncEnumerable<string> DownloadVideoOrAudioFromUrl(string url, [ParameterDescription("Option to download only audio when downloading a video")] bool onlyAudio)
        {
            if (!await Assistant.DangerCheck(nameof(DownloadVideoOrAudioFromUrl) + $"(\"{url}\", {onlyAudio})", FunctionTypes.VideoDownloadInteractions)) { yield return "Dangerous command canceled"; yield break; }
            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = '"' + url + '"' + (onlyAudio ? " -x" : ""),
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardOutput = true
            });
            if (process == null) { yield return "Failed download a video"; yield break; }
            while (!process.StandardOutput.EndOfStream)
            {
                char[] buffer = new char[1024];
                await process.StandardOutput.ReadAsync(buffer.AsMemory());
                foreach (char c in buffer) yield return c.ToString();
            }
        }
    }
}