using AiAssistant_Installer.Github;
using System.IO.Compression;
using System.Timers;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
                        Task.Run(() => CheckForUpdate("Vader0pr", "AI-Assistant", versions.AiAssistant ?? "", FileType.Zip, 1, OperatingSystem.IsWindows() ? @"Console-win64\.zip" : @"Console-linux64\.zip")),
                        Task.Run(() => CheckForUpdate("BtbN", "FFmpeg-Builds", versions.Ffmpeg ?? "", FileType.Zip, 2, OperatingSystem.IsWindows() ? @"ffmpeg-master-latest-win64-gpl\.zip" : @"ffmpeg-master-latest-linux64-gpl\.tar\.xz", ffmpeg: true)),
                        Task.Run(() => CheckForUpdate("yt-dlp", "yt-dlp", versions.Ytdlp ?? "", FileType.Exe, 3, OperatingSystem.IsWindows() ? @"yt-dlp\.exe" : @"yt-dlp(?!.)")),
                    ];
                    System.Timers.Timer timer = new(1000);
                    timer.Elapsed += Timer_Elapsed;
                    timer.Start();
                    await Task.WhenAll(releaseTasks);
                    timer.Stop();
                    if (OperatingSystem.IsWindows())
                    {
                        var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                        if (path != null && !path.Contains(Environment.CurrentDirectory)) Environment.SetEnvironmentVariable("PATH", path + Environment.CurrentDirectory + ";", EnvironmentVariableTarget.User);
                    }
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
            Console.Read();
        }
        static async Task<string?> CheckForUpdate(string owner, string repo, string version, FileType fileType, int downloadId, string filter, bool ffmpeg = false)
        {
            using (GithubApiClient client = new())
            {
                var release = await client.GetLatestReleaseAsync(owner, repo);
                if (release == null || release.Assets == null) return null;
                if (release.Name != version) await Install(repo, fileType, downloadId, filter, release.Assets, ffmpeg);
                else Messages.Add($"{owner}/{repo} already has the latest version");
                return release.Name;
            }
        }
        static async Task Install(string repo, FileType fileType, int downloadId, string filter, ReleaseAsset[] assets, bool ffmpeg = false)
        {
            IQueryable<ReleaseAsset> assetsQuery = assets.AsQueryable().Where(x => Regex.IsMatch(x.Name ?? "", filter));
            ReleaseAsset asset = assetsQuery.First();

            string assetName = asset.Name ?? repo + ".zip";
            string? folderName = assetName.Split('.').Length > 1 ? assetName.Split('.')[0] : null;

            if (File.Exists(asset.Name)) File.Delete(asset.Name);
            if (folderName != null && Directory.Exists(folderName)) Directory.Delete(folderName, true);

            using (HttpClient client = new())
            {
                await using (FileStream fs = new(assetName, FileMode.Create, FileAccess.Write))
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
            if (fileType == FileType.Zip && !ffmpeg && folderName != null)
            {
                ZipFile.ExtractToDirectory(assetName, folderName);
                File.Delete(assetName);
                foreach (string file in Directory.EnumerateFiles(folderName)) File.Move(file, Path.Combine(Environment.CurrentDirectory, new FileInfo(file).Name), true);
                Directory.Delete(folderName, true);
            }
            if (ffmpeg && folderName != null)
            {
                string path = Path.Combine(folderName, "bin");
                if (OperatingSystem.IsWindows() || assetName.EndsWith(".zip")) ZipFile.ExtractToDirectory(assetName, "");
                else if (assetName.EndsWith("tar.xz"))
                {
                    Process? extract = Process.Start(new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"tar -xf {assetName}\"",
                        UseShellExecute = false
                    });
                    if (extract != null) await extract.WaitForExitAsync();
                }
                foreach (string file in Directory.EnumerateFiles(path))
                {
                    string destination = Path.Combine(Environment.CurrentDirectory, new FileInfo(file).Name);
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