using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using Serilog;

namespace HoscyCore.Services.Core;

public abstract class SoloModuleManagerBaseV2<TModuleStartInfo, TModule, TLog>
(
    IBackToFrontNotifyService notify,
    ILogger logger,
    IContainerBulkLoader<TModuleStartInfo> infoLoader,
    IContainerBulkLoader<TModule> moduleLoader
)
: StartStopServiceBase, ISoloModuleManagerV2<TModuleStartInfo>
    where TModuleStartInfo : class, ISoloModuleStartInfo
    where TModule : class, IStartStopModule
{
    #region Injected
    protected readonly IBackToFrontNotifyService _notify = notify;
    protected readonly ILogger _logger = logger.ForContext<TLog>();
    private readonly IContainerBulkLoader<TModuleStartInfo> _infoLoader = infoLoader;
    private readonly IContainerBulkLoader<TModule> _moduleLoader = moduleLoader;
    #endregion

    #region Service Variables
    protected TModule? _currentModule;
    private readonly List<TModuleStartInfo> _moduleInfos = [];
    private Exception? _moduleChangeException = null;
    #endregion

    #region Info
    public IReadOnlyList<TModuleStartInfo> GetModuleInfos()
    {
        return _moduleInfos;
    }

    public TModuleStartInfo? GetCurrentModuleInfo()
    {
        if (_currentModule is null)
            return null;
        
        var searchType = _currentModule.GetType();
        var returnType = SearchInfos(x => x.ModuleType == searchType, $"Type={searchType.Name}");

        if (returnType is null)
        {
            _notify.SendError("Error retrieving active module info", $"Unable to locate current module ({searchType.Name}) in info list");
            _logger.Error("Unable to locate info for current module ({module}) in info list", searchType.FullName);
        }
        return returnType;
    }

    private TModuleStartInfo? SearchInfos(Predicate<TModuleStartInfo> search, string searchInfoForLog)
    {
        var infoSearch = _moduleInfos.Where(search.Invoke).ToArray();

        if (infoSearch.Length == 0)
        {
            _logger.Error("No module info found with search info {searchInfo}", searchInfoForLog);
            return null;
        }

        if (infoSearch.Length > 1)
        {
            var moduleOptions = string.Join(", ", infoSearch.Select(x => $"{x.Name} ({x.ModuleType.Name})"));
            _logger.Warning("Multiple modle infos found with search info {searchInfo}: {moduleOptions} => picking first", searchInfoForLog);
        }

        return infoSearch[0];
    }

    public ServiceStatus GetCurrentModuleStatus()
    {
        return _currentModule?.GetCurrentStatus() ?? ServiceStatus.Stopped;
    }
    #endregion

    #region Start / Stop
    protected override void StartInternal()
    {
        _logger.Debug("Starting up Service - Loading available ModuleStartInfos and perform module refresh");
        if (IsStarted())
        {
            _logger.Debug("Skipped starting Service, still running");
            return;
        }

        _moduleInfos.Clear();
        _moduleInfos.AddRange(_infoLoader.GetInstances());

        if (_moduleInfos.Count == 0)
        {
            _logger.Warning("No ModuleStartInfos could be located, Service will have no functionality and will be NOT be marked as running");
            return;
        }

        if (ShouldStartModelOnStartup())
        {
            _logger.Debug("Starting up model on startup");
            StartModule();
        }
        _logger.Debug("Started up Service with {moduleCount} ModuleStartInfos and module refresh", _moduleInfos.Count);
    }

    public override void Stop()
    {
        _logger.Debug("Stopping service, shutting down Module");
        StopModule();
        _moduleInfos.Clear();
        _logger.Debug(messageTemplate: "Stopped service, shut down Module");
    }

    public override void Restart()
    {
        RestartSimple(GetType(), _logger);
    }

    protected override bool IsStarted()
        => _moduleInfos.Count > 0;
    protected override bool IsProcessing()
        => IsStarted() && _currentModule is not null;
    #endregion

    #region Module => Start / Stop
    public bool StartModule()
    {
        return CallModuleStateChangeSafe(StartModuleInternal, "start");
    }
    private void StartModuleInternal()
    {
        var selectedModuleName = GetSelectedModuleName();
        _logger.Information("Attemtping to start module with name \"{name}\"", selectedModuleName);
        if (_currentModule is not null)
        {
            _logger.Information("Skipped starting Module, still running as \"{moduleType}\"", _currentModule.GetType().FullName);
            return;
        }

        var infoMatch = string.IsNullOrWhiteSpace(selectedModuleName)
            ? null 
            : SearchInfos(x => x.Name.Equals(selectedModuleName, StringComparison.OrdinalIgnoreCase),
                $"Name={selectedModuleName}");

        if (infoMatch is null)
        {
            _logger.Error("Failed to get module info with name \"{infoName}\"", selectedModuleName);
            throw new ArgumentException($"Failed to get module info with name \"{selectedModuleName}\"");
        }

        _logger.Debug("Attempting to locate module instance with \"{moduleName}\" and type \"{moduleType}\"",
            infoMatch.Name, infoMatch.ModuleType.Name);
        var moduleMatch = _moduleLoader.GetInstance(infoMatch.ModuleType);
        if (moduleMatch is null)
        {
            _logger.Error("Failed to get module instance name \"{moduleName}\" and type \"{moduleType}\"",
                infoMatch.Name, infoMatch.ModuleType.Name);
            throw new DiResolveException($"Failed to get module instance name {infoMatch.Name} and type {infoMatch.ModuleType.Name}");
        }

        moduleMatch.OnRuntimeError += HandleOnRuntimeError;
        moduleMatch.OnModuleStopped += HandleOnModuleStopped;
        OnModulePreStart(moduleMatch);
        moduleMatch.Start();
        OnModulePostStart(moduleMatch);
        _currentModule = moduleMatch;
        _logger.Information("Started module with name \"{moduleName}\" and type \"{moduleType}\"", infoMatch.Name, infoMatch.ModuleType.Name);
    }

    public bool StopModule()
    {
        return CallModuleStateChangeSafe(StopModuleInternal, "stop");
    }
    private void StopModuleInternal()
    {
        _logger.Information("Stopping current module");
        if (_currentModule is null)
        {
            _logger.Information("Skipping stopping of module, no module running");
            return;
        }

        _currentModule.OnModuleStopped -= HandleOnModuleStopped;
        OnModulePreStop(_currentModule);
        _currentModule.Stop();

        CleanupAfterModuleShutdown();
        _logger.Information("Stopped current module");
    }

    public bool RestartModule()
    {
        return CallModuleStateChangeSafe(RestartModuleInternal, "restart");
    }
    private void RestartModuleInternal()
    {
        _logger.Information("Restarting current module");
        
        if (_currentModule is null)
        {
            _logger.Information("Skipping restart of current module, no module running");
            return;
        }

        _currentModule.OnModuleStopped -= HandleOnModuleStopped;
        _currentModule.Restart();
        _currentModule.OnModuleStopped += HandleOnModuleStopped;

        _logger.Information("Restarted current module");
    }

    public bool CallModuleStateChangeSafe(Action stateChangeAction, string verb)
    {
        _moduleChangeException = null;
        try
        {
            stateChangeAction();
            return true;
        }
        catch (Exception ex)
        {
            SetAndNotifyModelChangeException(ex, $"Model {verb} failed", $"An exception occured while attempting to {verb} model");
            return false;
        }
    }

    private void HandleOnModuleStopped(object? sender, EventArgs e)
    {
        if (sender is null) return;
        _logger.Warning("HandleOnShutdownCompleted called for module of type \"{senderType}\", this should only happen when a shutdown was called unexpectedly", sender.GetType().FullName);
        CleanupAfterModuleShutdown();
    }

    private void CleanupAfterModuleShutdown()
    {
        if (_currentModule is not null)
        {
            _currentModule.OnRuntimeError -= HandleOnRuntimeError;
            _currentModule.OnModuleStopped -= HandleOnModuleStopped;
            OnModulePostStop(_currentModule);
            _currentModule = null;
        }
    }

    private void HandleOnRuntimeError(object? sender, Exception ex)
    {
        var moduleType = sender?.GetType();
        _logger.Error(ex, "Encountered an error in Module \"{moduleType}\"", moduleType?.FullName);
        _notify.SendError("Module error", $"Encountered an error in Module {moduleType?.FullName ?? "???"}", ex);
    }
    #endregion

    #region Errors
    public override Exception? GetFaultIfExists()
    {
        var exList = new List<Exception>();

        var baseException = base.GetFaultIfExists();
        if (baseException is not null)
        {
            exList.Add(baseException);
        }
        var moduleError = _currentModule?.GetFaultIfExists();
        if (moduleError is not null)
        {
            exList.Add(moduleError);
        }
        if (_moduleChangeException is not null)
        {
            exList.Add(_moduleChangeException);
        }

        return exList.Count == 0
            ? null
            : exList.Count > 1
                ? new CombinedException(exList)
                : exList[0];
    }

    private void SetAndNotifyModelChangeException(Exception ex, string title, string message, params object?[]? args)
    {
        _logger.Error(ex, message, args);
        _moduleChangeException = ex;
        _notify.SendError(title, message, ex);
    }
    #endregion

    #region Abstract
    protected abstract string GetSelectedModuleName();
    protected abstract bool ShouldStartModelOnStartup();

    protected virtual void OnModulePreStart(TModule module) { }
    protected virtual void OnModulePostStart(TModule module) { }
    protected virtual void OnModulePreStop(TModule module) { }
    protected virtual void OnModulePostStop(TModule module) { }
    #endregion
}