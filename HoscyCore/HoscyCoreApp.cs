using System.Runtime.InteropServices;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore;

public class HoscyCoreApp(ILogger? initialLogger = null)
{
    private ILogger _currentLogger = initialLogger?.ForContext<HoscyCoreApp>() ?? LogUtils.CreateTemporaryLogger<HoscyCoreApp>();
    private DiContainer? _container = null;

    public HoscyCoreApp Start(HoscyCoreAppStartParameters startParameters)
    {
        var onProgress = startParameters.OnProgress;
        var config = startParameters.PreloadedConfig;

        var version = LaunchUtils.GetVersion();
        _currentLogger.Warning("Starting HOSCY Version {hoscyVersion}", version);
        onProgress?.Invoke($"Loading HOSCY Version {version}");

        LogUtils.TryCleanLogs(PathUtils.PathExecutableFolder, _currentLogger);

        if (config is null)
        {
            onProgress?.Invoke("Loading Config");
            config = LaunchUtils.LoadConfigModel(_currentLogger, startParameters.CreateNewConfigIfMissing);
            if (config is null)
            {
                var ex = new ArgumentNullException("Unable to load config file");
                _currentLogger.Fatal(ex, "Unable to load config file");
                throw ex;
            }
        }

        if (startParameters.CreateLoggerFromConfiguration)
        {
            _currentLogger.Information("Switching to new logger");
            onProgress?.Invoke("Switching to new logger");
            _currentLogger = LogUtils.CreateLoggerFromConfiguration(config, startParameters.DisableConsoleLog).ForContext<HoscyCoreApp>();
            startParameters.OnNewLoggerCreated?.Invoke(_currentLogger);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && startParameters.ShouldOpenConsoleIfRequested && config.Logger_OpenWindowOnStartupWindowsOnly)
        {
            _currentLogger.Information("Starting Console");
            onProgress?.Invoke("Starting Console");
            WinApi.OpenConsole();
        }

        onProgress?.Invoke("Loading DI container");
        _container = DiContainer.LoadFromAssembly(_currentLogger, config, startParameters.AdditionalContainerInserts);

        _container.StartServices(onProgress);
        onProgress?.Invoke("Done!");
        return this;
    }

    public void Stop()
    {
        _currentLogger.Information("Shutting down HOSCY...");
        try
        {
            _container?.GetService<ConfigModel>()?.
                TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, _currentLogger);
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
