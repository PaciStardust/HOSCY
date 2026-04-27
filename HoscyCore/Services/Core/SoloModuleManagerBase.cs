using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Core;

public abstract class 
SoloModuleManagerBase<TModuleStartInfo, TModule>
(
    IBackToFrontNotifyService notify,
    ILogger logger,
    IContainerBulkLoader<TModuleStartInfo> infoLoader,
    IContainerBulkLoader<TModule> moduleLoader
)
: StartStopServiceBase(logger), ISoloModuleManager<TModuleStartInfo>
    where TModuleStartInfo : class, ISoloModuleStartInfo
    where TModule : class, IStartStopModule
{
    #region Injected
    protected readonly IBackToFrontNotifyService _notify = notify;
    private readonly IContainerBulkLoader<TModuleStartInfo> _infoLoader = infoLoader;
    private readonly IContainerBulkLoader<TModule> _moduleLoader = moduleLoader;
    #endregion

    #region Service Variables
    protected TModule? _currentModule;
    private readonly List<TModuleStartInfo> _moduleInfos = [];
    private readonly List<ResMsg> _moduleChangeErrorMessages = [];
    #endregion

    #region Info
    public IReadOnlyList<TModuleStartInfo> GetModuleInfos()
    {
        return _moduleInfos;
    }

    public Res<TModuleStartInfo>? GetCurrentModuleInfo()
    {
        if (_currentModule is null)
            return null;
        
        var searchType = _currentModule.GetType();
        var returnType = SearchInfos(x => x.ModuleType == searchType, $"Type={searchType.Name}");

        if (!returnType.IsOk)
        {
            _logger.Error("Unable to locate info for current module \"{module}\" in info list", searchType.FullName);
            return ResC.TFail<TModuleStartInfo>(ResMsg.Err($"Unable to locate info for current module \"{searchType.Name}\" in info list"));
        }

        return returnType;
    }

    private Res<TModuleStartInfo> SearchInfos(Predicate<TModuleStartInfo> search, string searchInfoForLog)
    {
        var infoSearch = _moduleInfos.Where(search.Invoke).ToArray();
        if (infoSearch.Length == 0)
        {
            _logger.Error("No module info found with search info {searchInfo}", searchInfoForLog);
            return ResC.TFail<TModuleStartInfo>(ResMsg.Err("No module info found"));
        }

        if (infoSearch.Length > 1)
        {
            var moduleOptions = string.Join(", ", infoSearch.Select(x => $"{x.Name} ({x.ModuleType.Name})"));
            _logger.Warning("Multiple module infos found with search info {searchInfo}: {moduleOptions} => picking first", searchInfoForLog);
        }

        return ResC.TOk(infoSearch[0]);
    }

    public ServiceStatus GetCurrentModuleStatus()
    {
        return _currentModule?.GetCurrentStatus() ?? ServiceStatus.Stopped;
    }
    #endregion

    #region Start / Stop
    protected override Res StartForService()
    {
        _logger.Debug("Loading available ModuleStartInfos and perform module refresh");
        _moduleInfos.Clear();

        var instances = _infoLoader.GetInstances();
        if (!instances.IsOk) return ResC.Fail(instances.Msg);

        if (instances.Value.Count == 0)
        {
            var msg = ResMsg.Wrn("No ModuleStartInfos could be located, Service will have no functionality and will be NOT be marked as running");
            SetFaultLogNotify(msg, "Failed to load Modules", null, _logger);
            return ResC.Ok();
        }
        _moduleInfos.AddRange(instances.Value);

        if (ShouldStartModelOnStartup() && !string.IsNullOrWhiteSpace(GetSelectedModuleName()))
        {
            _logger.Debug("Starting up model on startup");
            var moduleStart = StartModule();

            if (!moduleStart.IsOk)
            {
                _logger.Error("Module startup failed ({result})", moduleStart);
            }
        }

        _logger.Debug("Started up Service with {moduleCount} ModuleStartInfos and module refresh", _moduleInfos.Count);
        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForService() 
    {
        return StopModule();
    }
    protected override void DisposeCleanup()
    {
        _moduleInfos.Clear();
    }

    protected override bool IsStarted()
        => _moduleInfos.Count > 0;
    protected override bool IsProcessing()
        => IsStarted() && _currentModule is not null;
    #endregion

    #region Module => Start / Stop
    public Res StartModule()
    {
        return CallModuleStateChangeSafe(StartModuleInternal, "start");
    }
    private Res StartModuleInternal()
    {
        var selectedModuleName = GetSelectedModuleName();
        _logger.Information("Attemtping to start module with name \"{name}\"", selectedModuleName);

        if (string.IsNullOrWhiteSpace(selectedModuleName))
        {
            _logger.Information("No module is selected, not starting");
            return ResC.Ok();
        }

        var infoMatch = SearchInfos(x => x.Name.Equals(selectedModuleName, StringComparison.OrdinalIgnoreCase),
            $"Name={selectedModuleName}");
        if (!infoMatch.IsOk)
            return ResC.FailLog($"Failed to get module info with name \"{selectedModuleName}\"", _logger);

        var info = infoMatch.Value;
        if (_currentModule is not null)
        {
            var currentType = _currentModule.GetType();
            _logger.Information("Skipped starting Module \"{newModuleType}\", still running as \"{moduleType}\"",
                info.ModuleType.FullName, currentType.FullName);
            return currentType.GUID == info.ModuleType.GUID
                ? ResC.Ok()
                : ResC.Fail(ResMsg.Inf($"Skipped starting Module, still running as \"{currentType.Name}\""));
        }

        _logger.Debug("Attempting to locate module instance with \"{moduleName}\" and type \"{moduleType}\"",
            info.Name, info.ModuleType.FullName);

        var moduleMatch = _moduleLoader.GetInstance(info.ModuleType);
        if (!moduleMatch.IsOk)
        {
            _logger.Error("Failed to get module instance with name \"{moduleName}\" and type \"{moduleType}\"",
                info.Name, info.ModuleType.FullName);
            var message = ResMsg.Err($"Failed to get module instance with name \"{info.Name}\" and type \"{info.ModuleType.Name}\"");
            return ResC.Fail(message);
        }

        var module = moduleMatch.Value;
        module.OnRuntimeError += HandleOnRuntimeError;
        module.OnModuleStopped += HandleOnModuleStopped;

        var resPre = ResC.Wrap(() => OnModulePreStart(module), "Module Pre-Start failed", _logger);

        var res = resPre.IsOk 
            ? ResC.Wrap(module.Start, $"{selectedModuleName} Module Start failed", _logger) 
            : ResC.Fail($"{selectedModuleName} Module Pre-Start prerequisite failed");
        
        var resPost = res.IsOk 
            ? ResC.Wrap(() => OnModulePostStart(module), $"{selectedModuleName} Module Post-Start failed", _logger)
            : ResC.Fail($"{selectedModuleName} Module Post-Start prerequisite failed");

        if (!resPost.IsOk)
        {
            UnsubscribeFromModuleEvents(module);
            _logger.Error("Failed to start module with name \"{moduleName}\" and type \"{moduleType}\" ({result})",
                info.Name, info.ModuleType.FullName, res);
            return ResC.FailM(resPre.Msg, res.Msg, resPost.Msg);
        };

        _currentModule = module;
        _logger.Information("Started module with name \"{moduleName}\" and type \"{moduleType}\"",
            info.Name, info.ModuleType.FullName);
        OnModulePostAssign();

        return ResC.Ok();
    }

    public Res StopModule()
    {
        return CallModuleStateChangeSafe(StopModuleInternal, "stop");
    }
    private Res StopModuleInternal()
    {
        _logger.Information("Stopping current module");
        if (_currentModule is null)
        {
            _logger.Information("Skipping stopping of module, no module running");
            return ResC.Ok();
        }

        _currentModule.OnModuleStopped -= HandleOnModuleStopped;

        var resPre = ResC.Wrap(() => OnModulePreStop(_currentModule), "Module Pre-Stop failed", _logger);
        var res = ResC.Wrap(_currentModule.Stop, "Module Stop failed", _logger);
        CleanupAfterModuleShutdown();

        if (!resPre.IsOk || !res.IsOk)
        {
            var result = ResC.FailM(resPre.Msg?.WithContext("Pre-Stop"), res.Msg?.WithContext("Stop"));
            _logger.Warning("Stopped current module with problems ({result})", result);
            return result;
        }
        else
        {
            _logger.Information("Stopped current module");
            return ResC.Ok();
        }
    }

    public Res RestartModule()
    {
        return CallModuleStateChangeSafe(RestartModuleInternal, "restart");
    }
    private Res RestartModuleInternal()
    {
        _logger.Information("Restarting current module");
        
        if (_currentModule is null)
        {
            _logger.Information("Did not restart current module, no module running");
            return ResC.Ok();
        }

        _currentModule.OnModuleStopped -= HandleOnModuleStopped;
        var res = ResC.Wrap(_currentModule.Restart, "Module Restart", _logger);
        _currentModule.OnModuleStopped += HandleOnModuleStopped;

        if (!res.IsOk)
        {
            _logger.Error("Failed restarting current module ({result})", res);
        }
        else
        {
            _logger.Information("Restarted current module");
        }
        
        return res;
    }

    private Res CallModuleStateChangeSafe(Func<Res> stateChangeAction, string verb)
    {
        _moduleChangeErrorMessages.Clear();
        var res = ResC.Wrap(stateChangeAction, $"Model state change ({verb}) failed", _logger);
        if (!res.IsOk)
        {
            _moduleChangeErrorMessages.Add(res.Msg);
        }
        return res;
    }

    private void HandleOnModuleStopped(object? sender, EventArgs e)
    {
        if (sender is null) return;

        var type = sender.GetType();
        _logger.Warning("HandleOnShutdownCompleted called for module of type \"{senderType}\", this should only happen when a shutdown was called unexpectedly", type.FullName);
        
        CleanupAfterModuleShutdown();
        
        var msg = ResMsg.Wrn($"Unexpected shutdown of module of type \"{type.Name}\" occured");
        _moduleChangeErrorMessages.Add(msg);
        _notify.SendResult("Module shut down unexpectedly", msg);
    }

    private void CleanupAfterModuleShutdown()
    {
        if (_currentModule is not null)
        {
            _logger.Debug("Performing module post-shutdown cleanup");

            UnsubscribeFromModuleEvents(_currentModule);

            _currentModule = null;
            ResC.WrapR(OnModulePostStop, "Module Post-Stop failed", _logger);

            _logger.Debug("Performed module post-shutdown cleanup");
        }
    }

    private void UnsubscribeFromModuleEvents(TModule module)
    {
        UnsubscribeFromModuleEventsInternal(module);
        module.OnRuntimeError -= HandleOnRuntimeError;
        module.OnModuleStopped -= HandleOnModuleStopped;
    }
    protected virtual void UnsubscribeFromModuleEventsInternal(TModule module) { }

    private void HandleOnRuntimeError(object? sender, ResMsg msg)
    {
        var moduleType = sender?.GetType();
        _logger.Error("Encountered an error in Module \"{moduleType}\": {msg}", moduleType?.FullName, msg);
        _notify.SendResult("Module error", msg.WithContext($"Error in Module \"{moduleType?.Name ?? "???"}\""));
    }
    #endregion

    #region Errors
    public override ResMsg? GetErrorMessageIfExists()
    {
        var exList = new List<ResMsg>();

        var baseException = base.GetErrorMessageIfExists();
        if (baseException is not null)
        {
            exList.Add(baseException);
        }
        var moduleError = _currentModule?.GetErrorMessageIfExists();
        if (moduleError is not null)
        {
            exList.Add(moduleError);
        }
        if (_moduleChangeErrorMessages.Count > 0)
        {
            exList.AddRange(_moduleChangeErrorMessages);
        }

        return exList.Count == 0
            ? null
            : exList.Count > 1
                ? ResMsg.Combine(exList)
                : exList[0];
    }
    #endregion

    #region Abstract
    protected abstract string GetSelectedModuleName();
    protected abstract bool ShouldStartModelOnStartup();

    protected virtual Res OnModulePreStart(TModule module) { return ResC.Ok(); }
    protected virtual Res OnModulePostStart(TModule module) { return ResC.Ok(); }
    protected virtual void OnModulePostAssign() { }
    protected virtual Res OnModulePreStop(TModule module) { return ResC.Ok(); }
    protected virtual void OnModulePostStop() { }
    #endregion
}