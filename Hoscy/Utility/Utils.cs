using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Serilog;

namespace Hoscy.Utility;

public static class Utils
{
    public static string PathExecutable { get; private set; }
    public static string PathConfigFolder { get; private set; }
    public static string PathConfigFile { get; private set; }
    public static string PathExecutableFolder { get; private set; }
    public static string PathLog { get; private set; }

    static Utils()
    {
        PathExecutable = Process.GetCurrentProcess().MainModule?.FileName ?? Assembly.GetExecutingAssembly().Location;
        PathExecutableFolder = Path.GetDirectoryName(PathExecutable) ?? Directory.GetCurrentDirectory();
        PathConfigFolder = Path.GetFullPath(Path.Combine(PathExecutableFolder, "config"));
        PathConfigFile = Path.GetFullPath(Path.Combine(PathConfigFolder, "config.json"));
        PathLog = Path.GetFullPath(Path.Combine(PathConfigFolder, $"log-{DateTime.Now:MM-dd-yyyy-HH-mm-ss}.txt"));
    }

    #region Extention Methods
    /// <summary>
    /// Runs an async Task without awaiting
    /// </summary>
    /// <param name="task">Task to be run</param>
    public static void RunWithoutAwait(this Task task)
        => Task.Run(async () => await task).ConfigureAwait(false);

    /// <summary>
    /// Makes the first character of a string into an uppercase char
    /// </summary>
    /// <param name="input">String to modify</param>
    /// <returns>Modified string</returns>
    public static string FirstCharToUpper(this string input) =>
        string.IsNullOrEmpty(input) ? input : string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));

    /// <summary>
    /// Returns index of first element matching predicate
    /// </summary>
    public static int GetListIndex<T>(this IList<T> list, Predicate<T> predicate)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
                return i;
        }
        return -1;
    }
    #endregion

    #region Extra functions
    /// <summary>
    /// Starts a process
    /// </summary>
    public static bool StartProcess(string path, ILogger logger)
    {
        try
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = path,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to start process.");
            return false;
        }
    }

    /// <summary>
    /// Extracts a json field from a string
    /// </summary>
    /// <param name="name">Name of the field to search</param>
    /// <param name="json">The text inside the field or string.Empty if unavailable</param>
    /// <returns></returns>
    public static string? ExtractFromJson(string name, string json)
    {
        string regstring = name + @""" *: *""(?<value>([^""\\]|\\.)*)""";
        var regex = new Regex(regstring, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var result = regex.Match(json)?.Groups["value"].Value ?? null;
        return string.IsNullOrWhiteSpace(result) ? null : Regex.Unescape(result);
    }

    /// <summary>
    /// Recursive function to return the folder that is likely holding the main model (More than 1 inner folder)
    /// </summary>
    /// <param name="folderName">Path of folder to search</param>
    /// <returns>Innermost folder</returns>
    public static string GetActualContentFolder(string folderName)
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
    public static T MinMax<T>(T value, T min, T max) where T : IComparable
    {
        if (value.CompareTo(min) < 0)
            return min;
        if (value.CompareTo(max) > 0)
            return max;
        return value;
    }
    #endregion

    #region API
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    /// <summary>
    /// Launches an error box on windows, any other platform is hacky
    /// (feel free to prove me otherwise)
    /// If you use this software on anything but windows I expect you also could figure this out without a message box :3 
    /// </summary>
    public static void ShowErrorBoxOnWindows(string message, string title = "HOSCY - Error")
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _ = MessageBoxW(IntPtr.Zero, message, title, 0x10);
        }
    }

    [DllImport("Kernel32")]
    private static extern void AllocConsole();

    /// <summary>
    ///  Opens a console in Windows, other OS should just launch over command line to have logging
    /// </summary>
    public static void OpenConsoleOnWindows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AllocConsole();
        }
    }
    #endregion
}