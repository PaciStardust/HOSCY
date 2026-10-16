using System.Reflection;
using Serilog;

namespace HoscyCore.Utility;

/// <summary>
/// Utilities for Important paths
/// </summary>
public static class PathUtils
{
    //public static string PathConfigFolder { get; private set; }
    //public static string PathExecutableFolder { get; private set; }

    public static string PathConfig { get; private set; }
    public static string PathLog { get; private set; }
    public static string PathHoscyApp { get; private set; }

    static PathUtils()
    {
        var pathExecutable = Assembly.GetExecutingAssembly().Location;
        PathHoscyApp = Path.GetFullPath(Path.GetDirectoryName(pathExecutable) ?? Directory.GetCurrentDirectory());

        var envAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        PathConfig = Path.Combine(envAppData ?? PathHoscyApp, "hoscy");

        var envAppDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        PathLog = Path.Combine(envAppDataLocal ?? PathHoscyApp, "hoscy");
    }

    /// <summary>
    /// Recursive function to return the folder that is likely holding the main model (More than 1 inner folder)
    /// </summary>
    /// <param name="folderName">Path of folder to search</param>
    /// <returns>Innermost folder</returns>
    public static Res<string> GetActualContentFolder(string folderName, ILogger logger)
    {
        try
        {
            var subDirs = Directory.GetDirectories(folderName);
            var files = Directory.GetFiles(folderName).Length;
            var countSub = subDirs.Length;

            if (files != 0)
                return ResC.TOk(folderName);

            if (countSub == 0)
                return ResC.TFailLog<string>($"Failed to locate content folder in \"{folderName}\"", logger);
            else if (countSub == 1)
                return GetActualContentFolder(subDirs[0], logger);
            else
                return ResC.TOk(folderName);
        }
        catch (Exception ex)
        {
            return ResC.TFailLog<string>($"Failed to locate content folder in \"{folderName}\"", logger, ex);
        }
    }
}