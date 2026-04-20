using System.Diagnostics;
using System.Timers;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Translation.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Output.Core;

[LoadIntoDiContainer(typeof(IOutputManagerService), Lifetime.Singleton)]
public class OutputManagerService //todo: [REFACTOR++] This should maybe be its own thread in the future if it causes delay
(
    ILogger logger,
    IBackToFrontNotifyService notify, 
    ConfigModel config,

    IContainerBulkLoader<IOutputPreprocessor> loadPreprocessors,
    IContainerBulkLoader<IOutputHandlerStartInfo> loadHandlerStartInfo,
    IContainerBulkLoader<IOutputHandler> loadOutputHandler,

    ITranslationManagerService translator
)
: StartStopServiceBase(logger.ForContext<OutputManagerService>()), IOutputManagerService
{ //todo: [REFACTOR?] Should this be made more resilient?
    #region Injected Classes
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly ConfigModel _config = config;

    private readonly IContainerBulkLoader<IOutputPreprocessor> _loadPreprocessors = loadPreprocessors;
    private readonly IContainerBulkLoader<IOutputHandlerStartInfo> _loadHandlerStartInfo = loadHandlerStartInfo;
    private readonly IContainerBulkLoader<IOutputHandler> _loadOutputHandler = loadOutputHandler;

    private readonly ITranslationManagerService _translator = translator;
    #endregion

    #region Operation Variables
    private readonly List<IOutputPreprocessor> _preprocessors = [];
    private readonly List<IOutputHandlerStartInfo> _handlerInfos = [];
    private readonly List<IOutputHandler> _activeHandlers = [];
    #endregion

    #region Events
    public event EventHandler<OutputMessageEventArgs> OnMessage = delegate { };
    public event EventHandler<OutputNotificationEventArgs> OnNotification = delegate {};
    public event EventHandler OnClear = delegate {};
    public event EventHandler<bool> OnProcessingIndicatorSet = delegate {};
    #endregion

    #region State
    protected override bool IsStarted()
        => _preprocessors.Count > 0 || _handlerInfos.Count > 0;
    protected override bool IsProcessing()
        => IsStarted() && _activeHandlers.Count > 0;

    private readonly List<ResMsg> _refreshExceptions = [];
    #endregion

    #region Start/Stop
    protected override Res StartForService()
    {
        _logger.Debug("Loading available OutputHandlerInfos");

        _handlerInfos.Clear();
        _preprocessors.Clear();

        var handlerStartInfoResult = _loadHandlerStartInfo.GetInstances();
        if (!handlerStartInfoResult.IsOk) return ResC.Fail(handlerStartInfoResult.Msg);

        if (handlerStartInfoResult.Value.Count == 0)
        {
            var msg = ResMsg.Wrn("No OutputHandlersInfos could be located, service will have no functionality and will be NOT be marked as running");
            SetFaultLogNotify(msg, title: "Failed to load Handlers", null, _logger);
            return ResC.Ok();
        }

        _handlerInfos.AddRange(handlerStartInfoResult.Value);

        _logger.Debug("Loading Preprocessors");
        var preprocessorResult = _loadPreprocessors.GetInstances();
        if (!preprocessorResult.IsOk) return ResC.Fail(preprocessorResult.Msg);

        _preprocessors.AddRange(preprocessorResult.Value.OrderBy(x => x.GetHandlingStage()));

        var refreshResult = RefreshHandlers();
        if (!refreshResult.IsOk)
        {
            _logger.Warning("Failed to refresh handlers correctly on launch ({result})", refreshResult);
        }

        _indicatorResetTimer ??= CreateIndicatorResetTimer();

        _logger.Debug("Loaded {handlerCount} OutputHandlerInfos ({activeCount} active) and {preprocessorCount} OutputPreprocessors",
            _handlerInfos.Count, _activeHandlers.Count, _preprocessors.Count);
        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForService()
    {
        StopIndicatorResetTimer();

        var activeHandlerCount = _activeHandlers.Count;
        _logger.Debug("Shutting down {activeHandlers} Handlers", activeHandlerCount);

        List<string> handlerErrors = [];
        for (var i = _activeHandlers.Count - 1; i >= 0; i--)
        {
            var handler = _activeHandlers[i];
            var res = ShutdownHandler(handler);
            if (!res.IsOk)
            {
                handlerErrors.Add(res.Msg?.WithContext(handler.Name).ToString() ?? $"Unspecified error from handler {handler.Name}");
            }
        }

        var stillActiveHandlers = _activeHandlers.Where(x => x.GetCurrentStatus() != ServiceStatus.Stopped).ToArray();
        if (stillActiveHandlers.Length > 0)
        {
            var notStoppedHandlers = string.Join(", ", stillActiveHandlers.Select(x => x.GetType().FullName));
            var message = $"Following Handlers failed to comply with a shutdown call: {notStoppedHandlers}";
            _logger.Error(message);
            handlerErrors.Add(message);
        }

        if (handlerErrors.Count > 0)
        {
            var combined = string.Join("\n", handlerErrors);
            var message = $"Handlers had the following errors while stopping:\n{combined}";
            return ResC.FailLog(message, _logger);
        }
        
        _logger.Debug("Shut down {activeHandlers} Handlers", activeHandlerCount);
        return ResC.Ok();
    }
    protected override void DisposeCleanup()
    {
        _activeHandlers.Clear();
        _handlerInfos.Clear();
        _preprocessors.Clear();
    }
    #endregion

    #region Handlers => Info
    public IReadOnlyList<IOutputHandlerStartInfo> GetHandlerInfos(bool activeOnly)
    {
        if (!activeOnly)
            return _handlerInfos;

        var returnInfos = new List<IOutputHandlerStartInfo>();
        foreach(var activeHandler in _activeHandlers)
        {
            var handlerType = activeHandler.GetType();
            var infoMatch = _handlerInfos.FirstOrDefault(x => x.ModuleType == handlerType);
            if (infoMatch is not null)
            {
                returnInfos.Add(infoMatch);
            }
            else
            {
                _logger.Warning("Could not find a matching info for active Handler of type \"{handlerType}\"",
                    handlerType.FullName);
            }
        }
        return returnInfos;
    }

    public ServiceStatus GetProcessorStatus(IOutputHandlerStartInfo handlerInfo)
    {
        var activeHandler = RetrieveActiveHandlerByType(handlerInfo.ModuleType);
        if (activeHandler is null)
        {
            return ServiceStatus.Stopped;
        } 

        var activeStatus = activeHandler.GetCurrentStatus();
        if (activeStatus == ServiceStatus.Stopped)
        {
            _logger.Warning("GetHandlerStatus: Retrieved stopped Handler with type \"{handlerType}\" from active list",
                handlerInfo.ModuleType.FullName);
        }
        return activeStatus;
    }
    #endregion

    #region Handlers => Internal Start / Stop Unsafe
    private Res ActivateHandler(IOutputHandlerStartInfo handlerInfo)
    {
        _logger.Information("Activating Handler with type \"{handlerType}\"", handlerInfo.ModuleType.FullName);
        var activeMatch = RetrieveActiveHandlerByType(handlerInfo.ModuleType);
        if (activeMatch is not null)
        {
            _logger.Debug("Terminating old Handler with type \"{handlerType}\"", handlerInfo.ModuleType.FullName);
            var shutdownRes = ShutdownHandler(activeMatch);
            if (!shutdownRes.IsOk)
            {
                _logger.Error("Failed to terminate old Handler with type \"{handlerType}\" ({result})",
                    handlerInfo.ModuleType.FullName, shutdownRes);
                return shutdownRes;
            }

            activeMatch = RetrieveActiveHandlerByType(handlerInfo.ModuleType);
            if (activeMatch is null)
            {
                _logger.Debug("Terminated old Handler with type \"{handlerType}\"", handlerInfo.ModuleType.FullName);
            }
            else
            {
                _logger.Error("Failed to terminate old Handler with type \"{handlerType}\"", handlerInfo.ModuleType.FullName);
                return ResC.Fail(ResMsg.Err($"Failed to terminate old Handler with type \"{handlerInfo.ModuleType.Name}\""));
            }
        }

        var newHandlerResult = RetrieveHandlerInstanceForType(handlerInfo.ModuleType);
        if (!newHandlerResult.IsOk) 
            return ResC.Fail(newHandlerResult.Msg);
        var newHandler = newHandlerResult.Value;

        newHandler.OnRuntimeError += HandleOnRuntimeError;
        newHandler.OnModuleStopped += HandleOnModuleStopped;

        var startRes = ResC.Wrap(newHandler.Start, "Handler start failed", _logger);
        if (!startRes.IsOk)
        {
            _logger.Error("Failed to activate Handler with type \"{handlerType}\" ({result})",
                handlerInfo.ModuleType.FullName, startRes);
            return startRes;
        }

        _activeHandlers.Add(newHandler);
        _logger.Information("Activated Handler with type \"{handlerType}\"", handlerInfo.ModuleType.FullName);
        return ResC.Ok();
    }

    private Res ShutdownHandler(IOutputHandler handler)
    {
        _logger.Information("Shutting down Handler with type \"{handlerType}\"", handler.GetType().FullName);
        ResC.WrapR(handler.Clear, "Clear on handler shutdown failed", _logger); // Result is not relevant here
        handler.OnModuleStopped -= HandleOnModuleStopped; //This is not needed when manually shutting down

        var res = ResC.Wrap(handler.Stop, "Handler shutdown failed", _logger);
        CleanupAfterHandlerShutdown(handler);
        
        if (res.IsOk)
            _logger.Information("Shut down Handler with type \"{handlerType}\"", handler.GetType().FullName);
        else
            _logger.Warning("Shut down Handler with type \"{handlerType}\" with error ({res})",
                handler.GetType().FullName, res);

        return res;
    }

    private Res RestartHandler(IOutputHandler handler)
    {
        _logger.Information("Restarting Handler with type \"{handlerType}\"", handler.GetType().FullName);
        handler.OnModuleStopped -= HandleOnModuleStopped;
        
        var res = handler.Restart();
        if (!res.IsOk)
        {
            _logger.Error("Failed restarting Handler with type \"{handlerType}\" ({result})",
                handler.GetType().FullName, res);
            return res;
        }
        
        handler.OnModuleStopped += HandleOnModuleStopped;
        _logger.Information("Restarted Handler with type \"{handlerType}\"", handler.GetType().FullName);
        
        return ResC.Ok();
    }
    #endregion

    #region Handlers => Internal Start / Stop Safe
    private void HandleOnModuleStopped(object? sender, EventArgs e)
    {
        if (sender is null) return;
        _logger.Warning("HandleOnModuleStopped called for type \"{senderType}\", this should only happen when a shutdown was called unexpectedly",
            sender.GetType().FullName);
        if (sender is not IOutputHandler handler) return;
        CleanupAfterHandlerShutdown(handler);
    }

    private void CleanupAfterHandlerShutdown(IOutputHandler handler)
    {
        handler.OnRuntimeError -= HandleOnRuntimeError;
        handler.OnModuleStopped -= HandleOnModuleStopped;
        _activeHandlers.Remove(handler);
    }
    #endregion

    #region Handlers => Public Control
    public Res RefreshHandlers()
    {
        _refreshExceptions.Clear();

        var diagnosticSw = Stopwatch.StartNew();
        _logger.Debug("Refreshing Output Handlers...");
        List<Type> permittedTypes = [];

        //Disabling and enabling all handlers
        foreach(var handlerInfo in _handlerInfos)
        {
            var match = RetrieveActiveHandlerByType(handlerInfo.ModuleType);
            if (handlerInfo.ShouldBeEnabled())
            {
                if (match is null)
                {
                    _logger.Debug("Handler of type \"{handlerType}\" is enabled but not active, starting...",
                        handlerInfo.ModuleType);
                    var res = ActivateHandler(handlerInfo);
                    AddRefreshExceptionIfMessage(res, handlerInfo.ModuleType.Name);
                }
                permittedTypes.Add(handlerInfo.ModuleType);
            } else
            {
                if (match is not null)
                {
                    _logger.Debug("Handler of type \"{handlerType}\" is disabled but active, stopping...",
                        handlerInfo.ModuleType);
                    var res = ShutdownHandler(match);
                    AddRefreshExceptionIfMessage(res, match.Name);
                }
            }
        }

        //Find stragglers
        for (var i = _activeHandlers.Count - 1; i >= 0; i--) // Has to be a for loop to avoid enumeration issues
        {
            var handler = _activeHandlers[i];
            var handlerType  = handler.GetType();
            if (permittedTypes.Contains(handlerType))
            {
                permittedTypes.Remove(handlerType);
                continue;
            }

            _logger.Warning("Handler of type \"{handlerType}\" is active but not on enabled list, stopping...",
                handlerType);
            var res = ShutdownHandler(handler);
            if (!res.IsOk)
            {
                _logger.Warning("Handler of type \"{handlerType}\" failed shutting down, removing from list forcefully - This should not be ignored!",
                    handlerType);
                AddRefreshExceptionIfMessage(res, handler.Name);
                CleanupAfterHandlerShutdown(handler);
            }
        }

        diagnosticSw.Stop();
        _logger.Debug("Finished refreshing Output Handlers in {timeMs}ms", diagnosticSw.ElapsedMilliseconds);

        NotifyIfRefreshExceptions(); //todo: [REFACTOR] no more notify?
        return ResC.Ok(); //todo: [FIX] not always return ok
    }

    public Res RestartHandlers() 
    {
        _refreshExceptions.Clear();

        _logger.Debug("Restarting all {handlerCount} active Handlers...", _activeHandlers.Count);
        foreach(var handler in _activeHandlers)
        {
            var res = RestartHandler(handler);
            AddRefreshExceptionIfMessage(res, handler.Name);
        }
        _logger.Debug("Finished restarting all {handlerCount} active Handlers", _activeHandlers.Count);

        NotifyIfRefreshExceptions(); //todo: [REFACTOR] no more notify?
        return ResC.Ok(); //todo: [FIX] not always return ok
    }
    #endregion

    #region Handlers => Exception Handling
    private void HandleOnRuntimeError(object? sender, ResMsg msg)
    {
        var handlerType = sender?.GetType();
        _logger.Error("Encountered an error in Handler \"{handlerType}\": {msg}", handlerType?.FullName, msg);
        _notify.SendResult("Handler error", msg.WithContext($"Error in Handler \"{handlerType?.Name ?? "???"}\""));
    }
    #endregion

    #region Handlers => Utils
    private IOutputHandler? RetrieveActiveHandlerByType(Type handlerType)
    {
        var activeMatches = _activeHandlers.Where(x => x.GetType() == handlerType).ToArray();
        switch (activeMatches.Length)
        {
            case 0:
                return null;
            case 1:
                if (activeMatches[0].GetCurrentStatus() == ServiceStatus.Stopped)
                {
                    _logger.Warning("Handler with type \"{handlerType}\" was retrieved from active list despite being marked as stopped",
                        activeMatches[0].Name, handlerType.FullName);
                }
                return activeMatches[0];
            default:
                if (activeMatches.Any(x => x.GetCurrentStatus() == ServiceStatus.Stopped))
                {
                    _logger.Warning("One or multiple handlers retrieved from active list are marked as stopped");
                }
                _logger.Warning("Found multiple active {handlerCount} Handlers for type \"{type}\"", 
                    activeMatches.Length, handlerType.FullName);
                return activeMatches[0];
        }
    }

    private Res<IOutputHandler> RetrieveHandlerInstanceForType(Type type)
    {
        var searchMatch = _loadOutputHandler.GetInstance(type);
        if (!searchMatch.IsOk)
        {
            _logger.Error("Unable to retrieve Handler for type \"{handlerType}\"", type.FullName);
        }
        return searchMatch;
    }
    #endregion

    #region Handler => Control
    public void SendMessage(string contents, OutputSettingsFlags settings)
    {
        if (string.IsNullOrWhiteSpace(contents)) return;

        var compatiblePreprocessors = _preprocessors.Where(x => IsPreprocessorCompatible(x, settings)).ToArray();
        if (compatiblePreprocessors.Length > 0 && TryPreprocess(contents, compatiblePreprocessors, out var processedOutput))
        {
            if (string.IsNullOrWhiteSpace(processedOutput)) return;
            contents = processedOutput;
        }

        var compatibleHandlers = _activeHandlers
            .Where(x => IsHandlerCompatible(x, settings))
            .ToArray();
        if (compatibleHandlers.Length == 0)
        {
            OnMessage.Invoke(this, new(contents, [], null));
            _logger.Warning("Message with contents \"{message}\" was not handled as no handlers fit the criteria", contents);
            return;
        }

        if (settings.HasFlag(OutputSettingsFlags.DoTranslate) 
            && TryTranslateContentsIfNeeded(contents, compatibleHandlers, out var translatedText))
        {
            if (translatedText is null) return;
            SendMessageTranslatedInternal(contents, translatedText, compatibleHandlers);
        } 
        else
        {
            SendMessageInternal(contents, compatibleHandlers);
        }
    }

    private static bool IsHandlerCompatible(IOutputHandler handler, OutputSettingsFlags settings)
    {
        var id = handler.OutputTypeFlags;
        return (settings.HasFlag(OutputSettingsFlags.AllowTextOutput) && id.HasFlag(OutputsAsMediaFlags.OutputsAsText))
            || (settings.HasFlag(OutputSettingsFlags.AllowOtherOutput) && id.HasFlag(OutputsAsMediaFlags.OutputsAsOther))
            || (settings.HasFlag(OutputSettingsFlags.AllowAudioOutput) && id.HasFlag(OutputsAsMediaFlags.OutputsAsAudio));
    }

    private void SendMessageInternal(string contents, IOutputHandler[] handlers)
    {
        _logger.Verbose("Sending {handlerCount} handlers a message with contents \"{contentsMessage}\"",
            handlers.Length, contents);
        var handlerNames = handlers.Select(x => x.Name).ToArray();
        OnMessage.Invoke(this, new(contents, handlerNames, null));
        foreach (var handler in handlers)
        {
            handler.HandleMessage(contents);
        }
        _logger.Verbose("Sent {handlerCount} handlers a message with contents \"{contentsMessage}\"",
            handlers.Length, contents);
    }

    private void SendMessageTranslatedInternal(string contents, string translation, IOutputHandler[] handlers)
    {
        _logger.Verbose("Sending {handlerCount} handlers a message with contents \"{contentsMessage}\" and translation \"{translation}\"",
            handlers.Length, contents, translation);
        var handlerNames = handlers.Select(x => x.Name).ToArray();
        OnMessage.Invoke(this, new(contents, handlerNames, translation));
        foreach (var handler in handlers)
        {
            var newContents = handler.GetTranslationOutputMode() switch
            {
                OutputTranslationFormat.Translation => translation,
                OutputTranslationFormat.Untranslated => contents,
                OutputTranslationFormat.Both => $"{translation} / {contents}",
                _ => throw new ArgumentException("Unsupported TranslationOutputMode")
            };
            handler.HandleMessage(newContents);
        }
        _logger.Verbose("Sent {handlerCount} handlers a message with contents \"{contentsMessage}\" and translation \"{translation}\"",
            handlers.Length, contents, translation);
    }

    public void SendNotification(string contents, OutputNotificationPriority priority, OutputSettingsFlags settings)
    {
        if (string.IsNullOrWhiteSpace(contents)) return;

        var compatiblePreprocessors = _preprocessors.Where(x => IsPreprocessorCompatible(x, settings)).ToArray();
        if (compatiblePreprocessors.Length > 0 && TryPreprocess(contents, compatiblePreprocessors, out var processedOutput))
        {
            if (string.IsNullOrWhiteSpace(processedOutput)) return;
            contents = processedOutput;
        }

        var compatibleHandlers = _activeHandlers
            .Where(x => IsHandlerCompatible(x, settings))
            .ToArray();
        if (compatibleHandlers.Length == 0)
        {
            OnNotification.Invoke(this, new(contents, [], priority));
            _logger.Warning("Notification with contents \"{message}\" was not handled as no handlers fit the criteria", contents);
            return;
        }

        _logger.Verbose("Sending {handlerCount} handlers a notification of priority {priority} with contents \"{contentsNotification}\"", 
            compatibleHandlers.Length, priority.ToString(), contents);
        var handlerNames = compatibleHandlers.Select(x => x.Name).ToArray();
        OnNotification.Invoke(this, new(contents, handlerNames, priority));
        foreach (var handler in compatibleHandlers)
        {
            handler.HandleNotification(contents, priority);
        }
        _logger.Verbose("Sent {handlerCount} handlers a notification of priority {priority} with contents \"{contentsNotification}\"",
            compatibleHandlers.Length, priority.ToString(), contents);
    }

    public void Clear()
    {
        _logger.Verbose("Sending {handlerCount} handlers a clear command", _activeHandlers.Count);
        OnClear(this, EventArgs.Empty);
        foreach (var handler in _activeHandlers)
        {
            handler.Clear();
        }
        _logger.Verbose("Sent {handlerCount} handlers a clear command", _activeHandlers.Count);
    }
    #endregion

    #region Processing Indicator
    private System.Timers.Timer? _indicatorResetTimer = null;
    public void SetProcessingIndicator(bool isProcessing)
    {
        if (_indicatorResetTimer is not null)
        {
            _indicatorResetTimer.Stop();
            if (isProcessing)
                _indicatorResetTimer.Start();
        }
        
        _logger.Verbose("Sending {handlerCount} handlers command to set processing indicator to {indicatorState}",
            _activeHandlers.Count, isProcessing);
        OnProcessingIndicatorSet(this, isProcessing);
        foreach (var handler in _activeHandlers)
        {
            handler.SetProcessingIndicator(isProcessing);
        }
        _logger.Verbose("Sent {handlerCount} handlers command to set processing indicator to {indicatorState}",
            _activeHandlers.Count, isProcessing);
    }

    private System.Timers.Timer CreateIndicatorResetTimer()
    {
        _logger.Debug("Creating indicator reset timer");

        System.Timers.Timer timer = new()
        {
            AutoReset = false,
            Interval = 5000
        };
        timer.Elapsed += OnIndicatorResetTimerElapsed;
        return timer;
    }

    private void StopIndicatorResetTimer()
    {
        _logger.Debug("Stopping IndicatorResetTimer");
        if (_indicatorResetTimer is not null)
        {
            _indicatorResetTimer.Stop();
            _indicatorResetTimer.Elapsed -= OnIndicatorResetTimerElapsed;
            _indicatorResetTimer.Dispose();
        }
    }

    private void OnIndicatorResetTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _logger.Verbose("Indicator automatically reset");
        SetProcessingIndicator(false);
    }
    #endregion


    #region Preprocessors
    /// <summary>
    /// Tries using preprocessors on text
    /// </summary>
    /// <param name="input">String to preprocess</param>
    /// <param name="output">Preprocessed string if success and not handled entirely by a processor</param>
    /// <returns>Success</returns>
    private bool TryPreprocess(string input, IOutputPreprocessor[] preprocessors, out string? output)
    {
        _logger.Verbose("Preprocessing \"{preProcessorInput}\" ...", input);
        string? currentOutput = null;
        foreach (var preprocessor in preprocessors)
        {
            if (!preprocessor.TryProcess(currentOutput ?? input, out var processedOutput)) continue;

            if (!preprocessor.ShouldContinueIfHandled())
            {
                _logger.Verbose("Preprocessor \"{preprocessorName}\" has done final handling on \"{preProcessorInput}\" with message \"{preProcessorOutput}\"", preprocessor.GetType().Name, input, processedOutput);
                output = null;
                return true;
            }

            _logger.Verbose("Preprocessor \"{preprocessorName}\" converted \"{currentInput}\" to \"{currentOutput}\"", currentOutput ?? input, processedOutput);
            currentOutput = processedOutput;
        }
        output = currentOutput;
        return output is not null;
    }

    private static bool IsPreprocessorCompatible(IOutputPreprocessor preprocessor, OutputSettingsFlags settings)
    {
        if (!preprocessor.IsEnabled())
            return false;

        return preprocessor.IsFullReplace()
            ? settings.HasFlag(OutputSettingsFlags.DoPreprocessFull)
            : settings.HasFlag(OutputSettingsFlags.DoPreprocessPartial);
    }
    #endregion

    #region Translation
    /// <summary>
    /// Tries to translate if needed
    /// </summary>
    /// <param name="contents">Text to translate</param>
    /// <param name="translatedText">Translated text if sucessfully translated, null if returning false or an error occured</param>
    /// <returns>Attempted translation?</returns>
    private bool TryTranslateContentsIfNeeded(string contents, IOutputHandler[] handlers, out string? translatedText)
    {
        if (!handlers.Any(x => x.GetTranslationOutputMode() != OutputTranslationFormat.Untranslated))
        {
            _logger.Warning("Attempted translation of message with contents \"{contents}\", but could not find a suitable output or translator", contents);
            translatedText = null;
            return !_config.Translation_SendUntranslatedIfUnavailable;
        }

        var result = _translator.TryTranslate(contents, out var translation);
        switch (result)
        {
            case TranslationResult.Succeeded:
                translatedText = translation;
                return true;
            case TranslationResult.UseOriginal:
                translatedText = null;
                return false;
            default:
            case TranslationResult.Failed:
                translatedText = null;
                return true;
        }
    }
    #endregion

    #region Errors
    public override ResMsg? GetErrorMessageIfExists()
    {
        var msgList = new List<ResMsg>();

        var baseException = base.GetErrorMessageIfExists();
        if (baseException is not null)
        {
            msgList.Add(baseException);
        }
        msgList.AddRange(_refreshExceptions);
        msgList.AddRange(GetHandlerExceptions());

        return msgList.Count == 0
            ? null
            : msgList.Count > 1
                ? ResMsg.Combine(msgList)
                : msgList[0];
    }

    private void AddRefreshExceptionIfMessage(ResBase res, string context)
    {
        res.IfFail(x => _refreshExceptions.Add(x.WithContext(context)));
    }

    private void NotifyIfRefreshExceptions() //todo: [REFACTOR] is this still needed?
    {
        if (_refreshExceptions.Count == 0) return;
        
        var ex = new CombinedException(_refreshExceptions.Select(x => new Exception(x.Message)).ToList());
        _logger.Warning(ex, "Following exceptions popped up during refresh");
        _notify.SendError("Errors ocurred during refresh", "The following errors occured while refreshing handlers", ex);
    }

    private List<ResMsg> GetHandlerExceptions()
    {
        var resMsgList = new List<ResMsg>();
        foreach (var handler in _activeHandlers)
        {
            var ex = handler.GetErrorMessageIfExists();
            if (ex is not null)
                resMsgList.Add(ex);
        }
        return resMsgList;
    }
    #endregion
}