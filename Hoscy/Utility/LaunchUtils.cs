using System;
using Hoscy.Configuration.Legacy;
using Hoscy.Configuration.Modern;
using Serilog;

namespace Hoscy.Utility;

public static class LaunchUtils
{
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