using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hoscy
{
    internal static class Utils
    {
        internal const string Github = "https://github.com/PaciStardust/HOSCY";
        internal const string GithubLatest = "https://api.github.com/repos/pacistardust/hoscy/releases/latest";

        internal static string PathExecutable { get; private set; }
        internal static string PathConfigFolder { get; private set; }
        internal static string PathConfigFile { get; private set; }
        internal static string PathLog { get; private set; }
        internal static string PathModels { get; private set; }

        static Utils()
        {
            PathExecutable = Process.GetCurrentProcess().MainModule?.FileName ?? Assembly.GetExecutingAssembly().Location;
            var exeFolder = Path.GetDirectoryName(PathExecutable) ?? Directory.GetCurrentDirectory();
            PathConfigFolder = Path.GetFullPath(Path.Combine(exeFolder, "config"));
            PathConfigFile = Path.GetFullPath(Path.Combine(PathConfigFolder, "config.json"));
            PathLog = Path.GetFullPath(Path.Combine(PathConfigFolder, $"log-{DateTime.Now:MM-dd-yyyy-HH-mm-ss}.txt"));
            PathModels = Path.GetFullPath(Path.Combine(PathConfigFolder, "models"));
        }

        /// <summary>
        /// Runs an async Task without awaiting
        /// </summary>
        /// <param name="function">Task to be run</param>
        internal static void RunWithoutAwait(Task function)
            => Task.Run(async () => await function).ConfigureAwait(false);

        /// <summary>
        /// Extracts a json field from a string
        /// </summary>
        /// <param name="name">Name of the field to search</param>
        /// <param name="json">The text inside the field or string.Empty if unavailable</param>
        /// <returns></returns>
        internal static string? ExtractFromJson(string name, string json)
        {
            string regstring = name + @""" *: *""(?<value>([^""\\]|\\.)*)""";
            var regex = new Regex(regstring, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            var result = regex.Match(json)?.Groups["value"].Value ?? null;
            return string.IsNullOrWhiteSpace(result) ? null : Regex.Unescape(result);
        }

        /// <summary>
        /// Gets the current version from the assembly
        /// </summary>
        /// <returns></returns>
        internal static string GetVersion()
        {
            var assembly = Assembly.GetEntryAssembly();
            return "v." + (assembly != null ? FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion : "???");
        }

        /// <summary>
        /// Recursive function to return the folder that is likely holding the main model (More than 1 inner folder)
        /// </summary>
        /// <param name="folderName">Path of folder to search</param>
        /// <returns>Innermost folder</returns>
        internal static string GetActualContentFolder(string folderName)
        {
            var subDirs = Directory.GetDirectories(folderName);
            var countSub = subDirs.Length;

            if (countSub == 0)
                return string.Empty;
            else if (countSub == 1)
                return GetActualContentFolder(subDirs[0]);
            else
                return folderName;
        }

        /// <summary>
        /// A combination of floor and ceil for comparables
        /// </summary>
        /// <typeparam name="T">Type to compare</typeparam>
        /// <param name="value">Value to compare</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>Value, if within bounds. Min, if value smaller than min. Max, if value larger than max. If max is smaller than min, min has priority</returns>
        internal static T MinMax<T>(T value, T min, T max) where T : IComparable
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }

        /// <summary>
        /// Gets the emedded ressource stream from the assembly by name
        /// </summary>
        /// <param name="name">Name of ressource</param>
        /// <returns>Stream of ressource</returns>
        internal static Stream? GetEmbeddedRessourceStream(string name)
            => Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
    }
}
