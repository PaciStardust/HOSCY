using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Serilog;

namespace HoscyCore.Utility;

public static class OtherUtils
{
    /// <summary>
    /// Starts a process
    /// </summary>
    public static Res<Process> StartProcess(string path, ILogger logger)
    {
        try
        {
            var proc = Process.Start(new ProcessStartInfo()
            {
                FileName = path,
                UseShellExecute = true
            });

            return proc is null || proc.HasExited
                ? ResC.TFailLog<Process>($"Process \"{path}\" could not be created for unknown reason", logger)
                : ResC.TOk(proc);
        }
        catch (Exception ex)
        {
            return ResC.TFailLog<Process>($"Failed to start process \"{path}\"", logger, ex);
        }
    }

    /// <summary>
    /// Tries to safely check if a process has exited, because for some reason HasExited can throw an exception
    /// </summary>
    public static bool HasProcessExitedSafe(Process process)
    {
        try
        {
            process.Refresh();
            return process.HasExited;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Extracts a json field from a string
    /// </summary>
    /// <param name="name">Name of the field to search</param>
    /// <param name="json">The text inside the field or string.Empty if unavailable</param>
    /// <returns></returns>
    public static Res<string> ExtractFromJson(string name, string json, ILogger logger)
    {
        try
        {
            string regstring = name + @""" *: *""(?<value>([^""\\]|\\.)*)""";
            var regex = new Regex(regstring, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            var resultMatch = regex.Match(json);
            if (!resultMatch.Success) 
                ResC.TFailLog<string>($"Unable to locate value for key \"{name}\" in json: {json}", logger, lvl: ResMsgLvl.Warning);

            return ResC.TOk(Regex.Unescape(resultMatch.Groups["value"].Value));
        }
        catch (Exception ex)
        {
            return ResC.TFailLog<string>($"Failed to extract with key \"{name}\" from json \"{json}\"", logger, ex);
        }
    }

    public static void ThrowOnInvalidPlatform(OSPlatform[] platforms)
    {
        var isCompatible = platforms.Any(RuntimeInformation.IsOSPlatform);
        if (!isCompatible)
        {
            var platformString = string.Join(", ", platforms.Select(x => x.ToString()));
            throw new PlatformNotSupportedException($"Feature not supported on this platform, only available on: {platformString}");
        }
    }

    /// <summary>
    /// Waits until something is no longer true
    /// </summary>
    /// <param name="waitIfTrue">Check</param>
    /// <param name="waitTotalMs">How long in total should be waited</param>
    /// <param name="intervalMs">Interval to check in</param>
    /// <returns>Success?</returns>
    public static bool WaitWhile(Func<bool> waitIfTrue, int waitTotalMs, int intervalMs)
    {
        var waitSteps = waitTotalMs / intervalMs;
        while(waitIfTrue() && waitSteps > 0)
        {
            waitSteps--;
            Thread.Sleep(5);
        }
        return !waitIfTrue();
    }

    /// <summary>
    /// Waits until something is no longer true
    /// </summary>
    /// <param name="waitIfTrue">Check</param>
    /// <param name="waitTotalMs">How long in total should be waited</param>
    /// <param name="intervalMs">Interval to check in</param>
    /// <returns>Success?</returns>
    public static async Task<bool> WaitWhileAsync(Func<bool> waitIfTrue, int waitTotalMs, int intervalMs, CancellationToken? ct = null)
    {
        var waitSteps = waitTotalMs / intervalMs;
        while(waitIfTrue() && waitSteps > 0 && ((!ct?.IsCancellationRequested) ?? true))
        {
            waitSteps--;
            await Task.Delay(5);
        }
        return !waitIfTrue();
    }
}