using System.Reflection;
using Serilog;

namespace HoscyCore.Utility;

/// <summary>
/// Utilities for Important paths
/// </summary>
public static class PathUtils
{
    public static string PathExecutable { get; private set; }
    public static string PathConfigFolder { get; private set; }
    public static string PathExecutableFolder { get; private set; }

    static PathUtils()
    {
        PathExecutable = Assembly.GetExecutingAssembly().Location;
        PathExecutableFolder = Path.GetDirectoryName(PathExecutable) ?? Directory.GetCurrentDirectory();
        PathConfigFolder = Path.GetFullPath(Path.Combine(PathExecutableFolder, "config"));
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
            var countSub = subDirs.Length;

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