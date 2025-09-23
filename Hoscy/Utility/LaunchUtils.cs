using System;
using System.Diagnostics;
using System.Reflection;
using Hoscy.Configuration.Legacy;
using Hoscy.Configuration.Modern;
using Serilog;

namespace Hoscy.Utility;

public static class LaunchUtils
{
    private const string UNKNOWN_VERSION = "???";
    private static string? _appVersion;
    /// <summary>
    /// Retrieves the current verion of the App from the assembly
    /// </summary>
    public static string GetVersion()
    {
        if (_appVersion is null)
        {
            try
            {
                var assembly = Assembly.GetEntryAssembly();
                _appVersion = "v." + (assembly != null ? FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion : UNKNOWN_VERSION);
            }
            catch
            {
                _appVersion = "v." + UNKNOWN_VERSION;
            }
        }
        return _appVersion;
    }

    /// <summary>
    /// Loads in the config model or generates a new one
    /// </summary>
    /// <returns>Null if creation fails</returns>
    public static ConfigModel? LoadConfigModel(ILogger logger)
    {

        ConfigModel? config;
        try
        {
            logger.Information("Attempting to load config file...");
            config = ConfigModelLoader.TryLoad(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, logger);
            if (config is null)
            {
                logger.Information("Could not find config file, attempting to load legacy config file instead...");
                config = LegacyConfigModelLoader.TryLoad(PathUtils.PathConfigFolder, LegacyConfigModelLoader.DEFAULT_FILE_NAME, logger)?
                .Upgrade(logger)
                .Migrate(logger);
            }
            if (config is null)
            {
                logger.Information("Could not find legacy config file, creating new file insted...");
                config = new();
            }
            config.Upgrade(logger);
            logger.Information("Successfully created and upgraded the provided configuration");
            config.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, logger);
            return config;
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Program wil shut down - Failed loading config file");
        }
        return null;
    }
}