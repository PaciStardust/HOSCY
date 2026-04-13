using System.Diagnostics;
using System.Reflection;
using HoscyCore.Configuration.Legacy;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using Serilog;
using Timer = System.Timers.Timer;

namespace HoscyCore.Utility;

/// <summary>
/// Utilities for Launching the Application
/// </summary>
public static class LaunchUtils
{
    private const string UNKNOWN_VERSION = "?.?.?";
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
                var assembly = Assembly.GetExecutingAssembly();
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
    public static Res<ConfigModel> LoadConfigModel(ILogger logger, bool createNewIfMissing = true)
    {
        try
        {
            logger.Information("Attempting to load config file...");
            var resConfig = ConfigModelLoader.TryLoad(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, logger);
            if (resConfig is not null && !resConfig.IsOk)
            {
                return resConfig;
            }

            var config = resConfig?.Value;
            if (config is null)
            {
                logger.Information("Could not find config file, attempting to load legacy config file instead...");

                var resLegacyConfig = LegacyConfigModelLoader.TryLoad(PathUtils.PathConfigFolder, LegacyConfigModelLoader.DEFAULT_FILE_NAME, logger);
                var resLegacyUpgrade = resLegacyConfig is null ? null : (resLegacyConfig.IsOk ? resLegacyConfig.Value.Upgrade(logger) : ResC.Fail(resLegacyConfig.Msg));

                if (resLegacyUpgrade is not null && !resLegacyUpgrade.IsOk)
                    return ResC.TFail<ConfigModel>(resLegacyUpgrade.Msg);

                config = resLegacyConfig?.Value?.Migrate(logger);
            }

            if (config is null)
            {
                if (!createNewIfMissing)
                {
                    return ResC.TFailLog<ConfigModel>("Could not find legacy config file, creation of new file is disabled, returning null",
                        logger, lvl: ResMsgLvl.Fatal);
                }
                logger.Information("Could not find legacy config file, creating new file insted...");
                config = new();
            }

            var resUpgrade = config.Upgrade(logger);
            if (!resUpgrade.IsOk)
            {
                logger.Fatal("Failed and upgrading the provided configuration ({res})", resUpgrade);
                return ResC.TFail<ConfigModel>(resUpgrade.Msg);
            }

            logger.Information("Successfully created and upgraded the provided configuration");
            config.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, logger);
            return ResC.TOk(config);
        }
        catch (Exception ex)
        {
            return ResC.TFailLog<ConfigModel>($"Failed loading config file", logger, ex, ResMsgLvl.Fatal);
        }
    }

    public static Res<IEnumerable<Type>> GetCompleteTypesFromAssemblies(ILogger logger)
    {
        try
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Distinct()
                .Where(x => !(x.IsAbstract || x.IsInterface));
            
            logger.Debug("Retrieved all types from assembly");
            return ResC.TOk(types);
        } 
        catch (Exception ex)
        {
            return ResC.TFailLog<IEnumerable<Type>>("Failed to retrieve types from assembly", logger, ex);
        }
    }

    /// <summary>
    /// Returns all implementations of a class that can be located in the provided container
    /// </summary>
    public static Res<List<T>> GetImplementationsInContainerForClass<T>(IServiceProvider container, ILogger logger)
    {
        List<T> instances = [];
        var searchType = typeof(T);
        logger.Debug("Locating instances of \"{baseType}\"", searchType.FullName);

        var allTypesResult = GetCompleteTypesFromAssemblies(logger);
        if (!allTypesResult.IsOk)
            return ResC.TFail<List<T>>(allTypesResult.Msg);

        try
        {
            foreach (var type in allTypesResult.Value)
            {
                if (!type.IsAssignableTo(searchType)) continue;
                var diType = type.GetCustomAttribute<LoadIntoDiContainerAttribute>()?.AsType ?? type;

                if (container.GetService(diType) is not T instance)
                {
                    logger.Debug("Could not locate instance of \"{baseType}\" \"{serviceType}\"", searchType.FullName, type.FullName);
                    continue;
                }
                logger.Verbose("Located instance of \"{baseType}\" \"{serviceType}\"", searchType.FullName, type.FullName);
                instances.Add(instance);
            }
        } 
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to retrieve instances of type \"{type}\" from type list", searchType.FullName);
            var message = ResMsg.Err(ResMsg.FmtEx(ex, $"Failed to retrieve instances of type \"{searchType.Name}\" from type list"));
            return ResC.TFail<List<T>>(message);
        }
        
        logger.Debug("Located {moduleCount} instances of \"{baseType}\"", instances.Count, searchType.FullName);
        return ResC.TOk(instances);
    }

    /// <summary>
    /// Creates and starts a timer to throw an exception in N ms
    /// </summary>
    private static Timer CreateTimerToThrowException(Exception exceptionToThrow, int msToThrowIn)
    {
        using var timer = new Timer(msToThrowIn)
        {
            AutoReset = false
        };
        timer.Elapsed += (_, _) => throw exceptionToThrow;
        timer.Start();
        return timer;
    }

    
    /// <summary>
    /// Waits for the provided task to end and throws an exception if it does not complete in N ms
    /// </summary>
    /// <param name="task">Task to be stopped</param>
    /// <param name="logger">Logger to log exception</param>
    /// <param name="msToWaitFor">Ms to wait before throwing exception</param>
    /// <param name="exceptionToThrow">Exception to throw when timing out</param>
    /// <returns>Exception if occured while waiting</returns>
    public static Res SafelyWaitForTaskWithTimeoutAndReturnException(Task? task, int msToWaitFor, Exception exceptionToThrow, ILogger logger)
    {
        try
        {
            using var timer = CreateTimerToThrowException(exceptionToThrow, msToWaitFor);
            task?.GetAwaiter().GetResult();
            timer.Stop();
            return ResC.Ok();
        }
        catch (Exception ex)
        {
            return ResC.FailLog("Timed out waiting for task to end", logger, ex);
        }
    }
}