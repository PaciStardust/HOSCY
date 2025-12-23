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
        PathExecutable = Process.GetCurrentProcess().MainModule?.FileName ?? Assembly.GetExecutingAssembly().Location;
        PathExecutableFolder = Path.GetDirectoryName(PathExecutable) ?? Directory.GetCurrentDirectory();
        PathConfigFolder = Path.GetFullPath(Path.Combine(PathExecutableFolder, "config"));
    }
}