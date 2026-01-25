using System.Diagnostics;
using System.Reflection;

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
}