using AiAssistant_Installer.Github;
using System.IO.Compression;
using System.Timers;

namespace AiAssistant_Installer
{
    internal sealed class Program
    {
        static int donwloadtime = 1;
        static readonly List<string> Messages = [];
        static async Task Main()
        {
            await StartInstaller();
        }
        static async Task StartInstaller()
        {
            var versions = await Versions.Load();
            using (GithubApiClient client = new())
            {
                try
                {
                    Task<string?>[] releaseTasks =
                    [
                        Task.Run(() => CheckForUpdate("Vader0pr", "AI-Assistant", versions.AiAssistant ?? "", FileType.Zip, 1, ["Console.zip"], [], mainFile: true)),
                        Task.Run(() => CheckForUpdate("BtbN", "FFmpeg-Builds", versions.Ffmpeg ?? "", FileType.Zip, 2, ["win64", "gpl", "zip"], ["shared", "linux"], true)),
                        Task.Run(() => CheckForUpdate("yt-dlp", "yt-dlp", versions.Ytdlp ?? "", FileType.Exe, 3, ["yt-dlp.exe"], [])),
                    ];
                    System.Timers.Timer timer = new(1000);
                    timer.Elapsed += Timer_Elapsed;
                    timer.Start();
                    await Task.WhenAll(releaseTasks);
                    timer.Stop();
                    var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                    if (path != null && !path.Contains(Environment.CurrentDirectory)) Environment.SetEnvironmentVariable("PATH", path + Environment.CurrentDirectory + ";");
                    versions.AiAssistant = releaseTasks[0].Result;
                    versions.Ffmpeg = releaseTasks[1].Result;
                    versions.Ytdlp = releaseTasks[2].Result;
                    await versions.Save();

                }
                catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
            }
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (string message in Messages) await Console.Out.WriteLineAsync(message);
            await Console.Out.WriteLineAsync("Installer finished working");
            Console.ForegroundColor = ConsoleColor.White;
            await Console.Out.WriteLineAsync("The installer will close in 5 seconds. Press any key to close now...");
            _ = Task.Run(async() =>
            {
                await Task.Delay(5000);
                Environment.Exit(0);
            });
            Console.ReadKey();
        }
        static async Task<string?> CheckForUpdate(string owner, string repo, string version, FileType fileType, int downloadId, string[] filter, string[] negativeFilter, bool ffmpeg = false, bool mainFile = false)
        {
            using (GithubApiClient client = new())
            {
                var release = await client.GetLatestReleaseAsync(owner, repo);
                if (release == null || release.Assets == null) return null;
                if (release.Name != version) await Install(repo, fileType, downloadId, filter, negativeFilter, release.Assets, ffmpeg, mainFile);
                else Messages.Add($"{owner}/{repo} already has the latest version");
                return release.Name;
            }
        }
        static async Task Install(string repo, FileType fileType, int downloadId, string[] filter, string[] negativeFilter, ReleaseAsset[] assets, bool ffmpeg = false, bool mainFile = false)
        {
            IQueryable<ReleaseAsset> assetsQuery = assets.AsQueryable();
            foreach (string item in filter) assetsQuery = assetsQuery.Where(x => (x.Name ?? "").Contains(item));
            foreach (string item in negativeFilter) assetsQuery = assetsQuery.Where(x => !(x.Name ?? "").Contains(item));
            ReleaseAsset asset = assetsQuery.First();

            string assetName = asset.Name ?? repo + ".zip";
            string folderName = assetName.Replace(new FileInfo(assetName).Extension, "");

            if (File.Exists(asset.Name)) File.Delete(asset.Name);
            if (Directory.Exists(folderName)) Directory.Delete(folderName, true);

            using (HttpClient client = new())
            {
                using (FileStream fs = new(assetName, FileMode.Create, FileAccess.Write))
                {
                    Stream stream = await client.GetStreamAsync(asset.DownloadUrl);
                    int totalBytesRead = 0;
                    Console.CursorVisible = false;
                    while (fs.Length < asset.Size)
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                        await fs.WriteAsync(buffer.AsMemory(0, bytesRead));
                        totalBytesRead += bytesRead;
                        string progress = "";
                        progress += "Downloading " + asset.Name + " [";
                        for (int i = 0; i < (int)((double)totalBytesRead / (double)asset.Size * 100); i += 10) progress += "X";
                        for (int i = 100; i > (int)((double)totalBytesRead / (double)asset.Size * 100); i -= 10) progress += " ";
                        progress += "]";
                        progress += $"{totalBytesRead / 1000000}/{asset.Size / 1000000}MB({(int)((double)totalBytesRead / (double)asset.Size * 100)}%) {Math.Round((float)totalBytesRead / 1000000 / donwloadtime, 1)}MB/s {donwloadtime}/{asset.Size / (totalBytesRead / donwloadtime)}s";
                        DownloadProgress.ReportProgress(downloadId, progress);
                    }
                }
            }
            if (fileType == FileType.Zip && !ffmpeg)
            {
                ZipFile.ExtractToDirectory(assetName, folderName);
                File.Delete(assetName);
                foreach (string file in Directory.EnumerateFiles(folderName)) File.Move(file, Path.Combine(Environment.CurrentDirectory, new FileInfo(file).Name), true);
                Directory.Delete(folderName, true);
            }
            if (ffmpeg)
            {
                string path = Path.Combine(folderName, folderName, "bin");
                ZipFile.ExtractToDirectory(assetName, folderName);
                foreach (string file in Directory.EnumerateFiles(path))
                {
                    string destination = Path.Combine(Environment.CurrentDirectory + "\\" + new FileInfo(file).Name);
                    File.Move(file, destination, true);
                }
                Directory.Delete(folderName, true);
                File.Delete(assetName);
            }
            DownloadProgress.ClearProgress(downloadId);
            DownloadProgress.RefreshProgress();
        }
        private enum FileType
        {
            Zip,
            Exe
        }
        private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            donwloadtime++;
            DownloadProgress.RefreshProgress();
        }
    }
}