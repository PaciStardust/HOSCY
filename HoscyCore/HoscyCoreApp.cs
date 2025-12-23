using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Utility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HoscyCore;

public class HoscyCoreApp(ILogger? initialLogger = null)
{
    private ILogger _currentLogger = initialLogger ?? LogUtils.CreateTemporaryLogger<HoscyCoreApp>();
    private ConfigModel? _config = null;
    private DiContainer? _container = null;

    public HoscyCoreApp SetConfigManually(ConfigModel config)
    {
        _currentLogger.Information("Config has been set manually");
        _config = config;
        return this;
    }

    public HoscyCoreApp Start(Action<string>? onProgress = null, bool createLoggerFromConfiguration = false, bool createNewConfigIfMissing = true, bool shouldOpenConsoleIfRequested = true, Action<ServiceCollection>? additionalContainerInserts = null)
    {
        var version = LaunchUtils.GetVersion();
        _currentLogger.Warning("Starting HOSCY Version {hoscyVersion}", version);
        onProgress?.Invoke($"Loading HOSCY Version {version}");

        if (_config is null)
        {
            onProgress?.Invoke("Loading Config");
            _config = LaunchUtils.LoadConfigModel(_currentLogger, createNewConfigIfMissing);
            if (_config is null)
            {
                var ex = new ArgumentNullException("Unable to load config file");
                _currentLogger.Fatal(ex, "Unable to load config file");
                throw ex;
            }
        }

        if (shouldOpenConsoleIfRequested && _config.Logger_OpenWindowOnStartupWindowsOnly)
        {
            _currentLogger.Information("Starting Console");
            onProgress?.Invoke("Starting Console");
            Utils.OpenConsoleOnWindows();
        }

        if (createLoggerFromConfiguration)
        {
            _currentLogger.Information("Switching to new logger");
            onProgress?.Invoke("Switching to new logger");
            _currentLogger = LogUtils.CreateLoggerFromConfiguration(_config);
        }

        onProgress?.Invoke("Loading DI container");
        _container = DiContainer.LoadFromAssembly(_currentLogger, _config, additionalContainerInserts);

        _container.StartServices(onProgress);
        return this;
    }

    public void Stop()
    {
        _currentLogger.Information("Shutting down HOSCY...");
        _config?.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, _currentLogger);
        try
        {
            _container?.StopServices();
        }
        catch (Exception ex)
        {
            _currentLogger.Fatal(ex, "Unable to stop Services correctly");
        }
        _currentLogger.Information("HOSCY has shut down, goodnight!");
    }

    public DiContainer GetContainer()
    {
        if (_container is not null)
        {
            return _container;
        }
        var ex = new ArgumentException("Container was requested but is not available");
        _currentLogger.Error(ex, "Failed to retrieve container");
        throw ex;
    }
}
