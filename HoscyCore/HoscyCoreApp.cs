using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore;

public class HoscyCoreApp(ILogger? initialLogger = null)
{
    private ILogger _currentLogger = initialLogger?.ForContext<HoscyCoreApp>() ?? LogUtils.CreateTemporaryLogger<HoscyCoreApp>();
    private DiContainer? _container = null;
    private HoscyCoreAppDebug? _debug = null;

    public Res<ResMsg[]> Start(HoscyCoreAppStartParameters startParameters)
    {
        var onProgress = startParameters.OnProgress;
        var config = startParameters.PreloadedConfig;

        var version = LaunchUtils.GetVersion();
        _currentLogger.Information("Starting HOSCY Version {hoscyVersion}", version);
        onProgress?.Invoke($"Loading HOSCY Version {version}");

        LogUtils.TryCleanLogs(PathUtils.PathExecutableFolder, _currentLogger);

        if (config is null)
        {
            onProgress?.Invoke("Loading Config");
            var configRes = LaunchUtils.LoadConfigModel(_currentLogger, startParameters.CreateNewConfigIfMissing);
            if (!configRes.IsOk) return ResC.TFail<ResMsg[]>(configRes.Msg);
            config = configRes.Value;
        }

        if (startParameters.CreateLoggerFromConfiguration)
        {
            _currentLogger.Information("Switching to new logger");
            onProgress?.Invoke("Switching to new logger");
            _currentLogger = LogUtils.CreateLoggerFromConfiguration(config, startParameters.DisableConsoleLog).ForContext<HoscyCoreApp>();
            startParameters.OnNewLoggerCreated?.Invoke(_currentLogger);
        }

        onProgress?.Invoke("Starting external logging");
        var debug = new HoscyCoreAppDebug(_currentLogger);
        var resDebug = debug.Start(startParameters, config);
        if (!resDebug.IsOk) return ResC.TFail<ResMsg[]>(resDebug.Msg);
        _debug = debug;

        onProgress?.Invoke("Loading DI container");
        var containerRes = DiContainer.CreateWithAssembly(_currentLogger, config, startParameters.AdditionalContainerInserts);
        if (!containerRes.IsOk) return ResC.TFail<ResMsg[]>(containerRes.Msg);
        _container = containerRes.Value;

        var startRes = _container.StartServices(onProgress);
        if (!startRes.IsOk) return ResC.TFail<ResMsg[]>(startRes.Msg);

        onProgress?.Invoke("Done!");
        return startRes;
    }

    public Res Stop()
    {
        List<ResMsg> stopErrors = [];

        _currentLogger.Information("Shutting down HOSCY...");
        if (_container is not null)
        {
            _container.GetService<ConfigModel>()?.
                TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, _currentLogger);
            _container.StopServices().IfFail((x) => stopErrors.Add(x.WithContext("StopServices")));
            _container = null;
        }

        _debug?.Stop().IfFail((x) => stopErrors.Add(x.WithContext("StopDebug")));
        _debug = null;

        if (stopErrors.Count == 0)
        {
            _currentLogger.Information("HOSCY has shut down, goodnight!");
            return ResC.Ok();
        }
        else
        {
            var result = ResC.FailM(stopErrors);
            _currentLogger.Fatal("HOSCY has shut down with errors, goodnight! ({result})", result);
            return result;
        }
    }

    public Res<DiContainer> GetContainer()
    {
        return _container is not null
            ? ResC.TOk(_container)
            : ResC.TFailLog<DiContainer>("Container was requested but is not available", _currentLogger);
    }
}