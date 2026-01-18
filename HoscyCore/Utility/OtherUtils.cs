using System.Diagnostics;
using System.Text.RegularExpressions;
using Serilog;

namespace HoscyCore.Utility;

public static class OtherUtils
{
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
}