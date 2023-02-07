using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Hoscy.Services.Api
{
    internal static class Updater
    {
        private const string _tempFileName = "hoscy-temp-download";

        /// <summary>
        /// Gets the latest version data from GitHub
        /// </summary>
        /// <returns></returns>
        private static async Task<string?> GetGithubLatest()
        {
            Logger.PInfo("Attempting to grab latest version data from GitHub");

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, Utils.GithubLatest);

            var res = await HoscyClient.SendAsync(requestMessage, notify: false);
            if (res == null)
                Logger.Warning("Failed to grab latest version data from GitHub");

            return res;
        }

        /// <summary>
        /// Checks if there is an update available, displays a window containing changelog
        /// </summary>
        internal static void CheckForUpdates()
           => Utils.RunWithoutAwait(CheckForUpdatesInternal());
        /// <summary>
        /// Internal version to avoid async hell
        /// </summary>
        private static async Task CheckForUpdatesInternal()
        {
            var currVer = Utils.GetVersion();
            Logger.PInfo("Attempting to check for newest HOSCY version, current is " + currVer);

            var res = await GetGithubLatest();
            if (res == null)
                return;

            var newVer = Utils.ExtractFromJson("tag_name", res);
            if (!string.IsNullOrWhiteSpace(newVer) && currVer != newVer)
            {
                var newBody = Utils.ExtractFromJson("body", res);
                Logger.Warning($"New version available (Latest is {newVer})");

                var notifText = $"Please update by selecting \"Update Tool\" in the config tab, or by redownloading from GitHub\n\nCurrent: {currVer}\nLatest: {newVer}{(string.IsNullOrWhiteSpace(newBody) ? string.Empty : $"\n\n{newBody}")}";
                App.OpenNotificationWindow("New version available", "A new version of HOSCY is available", notifText, true);
            }
            else
                Logger.Info("HOSCY is up to date");
        }

        /// <summary>
        /// Updates the tool
        /// </summary>
        internal static void PerformUpdate()
           => Utils.RunWithoutAwait(PerformUpdateInternal());
        /// <summary>
        /// Internal version to avoid async hell
        /// </summary>
        private static async Task PerformUpdateInternal()
        {
            var currVer = Utils.GetVersion();
            Logger.PInfo("Attempting to update to newest HOSCY version, current is " + currVer);

            var res = await GetGithubLatest();
            if (res == null)
                return;

            //Grabbing and validating version
            var newVer = Utils.ExtractFromJson("tag_name", res);
            if (string.IsNullOrWhiteSpace(newVer))
                return;
            else if (currVer == newVer)
            {
                Logger.Warning("Attempted to update, but already up to date!");
                App.OpenNotificationWindow("HOSCY up to date!", "Update cancelled", "HOSCY are already up to date");
                return;
            }

            //Grabbing download link from json
            var downloadLink = Utils.ExtractFromJson("browser_download_url", res);
            if (string.IsNullOrWhiteSpace(downloadLink))
            {
                Logger.Error("Unable to locate download link for new version");
                return;
            }

            //Checking if hoscy directory is available
            var hoscyDirectory = Directory.GetParent(Utils.PathExecutable);
            var downDirectory = hoscyDirectory?.Parent?.FullName;
            if (hoscyDirectory == null || downDirectory == null)
            {
                Logger.Error("Unable to find HOSCY parent folder");
                return;
            }

            //Download and Unzip
            Logger.PInfo("Attempting to grab new version and unzipping");
            downDirectory = Path.Combine(downDirectory, _tempFileName);
            var zipDirectory = downDirectory + ".zip";
            if (!await DownloadAndUnzipNew(downloadLink, zipDirectory, downDirectory))
                return;

            //Saving file and backing up
            Config.SaveConfig(true);

            //Creates and runs CMD
            Logger.PInfo("Creating and running update CMD command, HOSCY will now shutdown");
            try
            {
                var cmdCommand = CreateCmdCommand(hoscyDirectory, downDirectory);
                var pStartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd",
                    Arguments = cmdCommand,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(pStartInfo);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        #region Upgrade Parts
        /// <summary>
        /// Downloads file and unzips it
        /// </summary>
        /// <param name="url">Download url</param>
        /// <param name="zipPath">Path for Zip</param>
        /// <param name="unzipPath">Path for Unzip</param>
        /// <returns>Success?</returns>
        private static async Task<bool> DownloadAndUnzipNew(string url, string zipPath, string unzipPath)
        {
            try
            {
                if (File.Exists(zipPath))
                {
                    Logger.Info("Deleting preexisting zip-file");
                    File.Delete(zipPath);
                }

                //Trying download, cancelling if failed
                if (!await HoscyClient.DownloadAsync(url, zipPath))
                    return false;

                if (Directory.Exists(unzipPath))
                {
                    Logger.Info("Deleting preexisting downloaded directory");
                    Directory.Delete(unzipPath, true);
                }

                ZipFile.ExtractToDirectory(zipPath, unzipPath);
                File.Delete(zipPath);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                //Trying to delete old version of zip despite error
                try
                {
                    File.Delete(zipPath);
                }
                catch (Exception e2)
                {
                    Logger.Error(e2);
                }
            }
            return false;
        }

        /// <summary>
        /// Creates the CMD Command to close hoscy, move around the files, delete the temp folder and reopen hoscy
        /// </summary>
        /// <param name="hoscyDir">DirectoryInfo of the hoscy install directory</param>
        /// <param name="tempDirPath">Path of the temp download folder</param>
        /// <returns></returns>
        private static string CreateCmdCommand(DirectoryInfo hoscyDir, string tempDirPath)
        {
            var commands = new List<string>()
            {
                $"/C taskkill /f /im \"{Path.GetFileName(Utils.PathExecutable)}\"", //Getting current process name, killing it
                "timeout /t 5 /nobreak" //sleep 5s to hopefully fix any dll deletion issues
            };

            //adding all files and directories to be deleted from hoscy dir
            foreach (var file in hoscyDir.EnumerateFiles())
                commands.Add($"del \"{file.FullName}\"");
            foreach (var dir in hoscyDir.EnumerateDirectories())
            {
                if (dir.FullName != Utils.PathConfigFolder)
                    commands.Add($"rmdir /Q /S \"{dir.FullName}\"");
            }

            var tempDirContentPath = Utils.GetActualContentFolder(tempDirPath);
            commands.Add($"robocopy \"{tempDirContentPath}\" \"{hoscyDir.FullName}\" /E /XC /XN /XO & rmdir /Q /S \"{tempDirPath}\""); //Copy over but dont overwrite, Delete temp

            //Checking for new exe file
            var newExe = hoscyDir.GetFiles()
                .Where(x =>
                {
                    var xLow = x.Name.ToLower();
                    return xLow.EndsWith(".exe") && xLow.Contains("hoscy");
                });

            //If a new exe is found, run it
            if (newExe.Any())
                commands.Add($"\"{newExe.First()}\"");

            return string.Join(" && ", commands);
        }
        #endregion
    }
}
