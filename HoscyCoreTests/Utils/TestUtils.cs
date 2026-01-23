using System.Reflection;
using HoscyCore;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCoreTests.Utils;

public static class TestUtils
{
    private static readonly string _mainFolder = Path.Combine(Assembly.GetExecutingAssembly().Location, RESOURCE_FOLDER);

    private const string RESOURCE_FOLDER = "Resources";
    private const string TEMP_FOLDER = "TestTemp";

    public static string GetResourcePath(string resourceName)
    {
        return Path.Join(_mainFolder, RESOURCE_FOLDER, resourceName);
    }

    public static string GenerateTempFolder()
    {
        var path = Path.Join(_mainFolder, TEMP_FOLDER);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
        Directory.CreateDirectory(path);
        return path;
    }

    private static ILogger? _logger;
    public static ILogger GetLogger<T>()
    {
        if (_logger is null)
        {
            _logger = LogUtils.CreateTemporaryLogger<HoscyCoreApp>();
            LogUtils.TryCleanLogs(_mainFolder, _logger);
        }
        return _logger.ForContext<T>();
    }
}