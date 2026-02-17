using System.Diagnostics;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Translation.Core;
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

    ITranslatorManagerService translator
)
: StartStopServiceBase, IOutputManagerService
{
    #region Injected Classes
    private readonly ILogger _logger = logger.ForContext<OutputManagerService>();
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly ConfigModel _config = config;

    private readonly IContainerBulkLoader<IOutputPreprocessor> _loadPreprocessors = loadPreprocessors;
    private readonly IContainerBulkLoader<IOutputHandlerStartInfo> _loadHandlerStartInfo = loadHandlerStartInfo;
    private readonly IContainerBulkLoader<IOutputHandler> _loadOutputHandler = loadOutputHandler;

    private readonly ITranslatorManagerService _translator = translator;
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

    private readonly List<Exception> _refreshExceptions = [];
    #endregion

    #region Start/Stop
    protected override void StartInternal()
    {
        _logger.Debug("Starting up Service by loading available OutputHandlerInfos");
        if (IsStarted())
        {
            _logger.Debug("Skipped starting Service, still running");
            return;
        }

        _handlerInfos.Clear();
        _preprocessors.Clear();

        _handlerInfos.AddRange(_loadHandlerStartInfo.GetInstances());
        if (_handlerInfos.Count == 0)
        {
            _logger.Warning("No OutputHandlersInfos could be located, Service will have no functionality and will be NOT be marked as running");
            return;
        }

        _logger.Debug("Loading Preprocessors");
        var preprocessorsWithInstance = _loadPreprocessors.GetInstances();
        _preprocessors.AddRange(preprocessorsWithInstance.OrderBy(x => x.GetHandlingStage()));

        _logger.Debug("Refreshing Handlers");
        RefreshHandlers();

        _logger.Debug("Started up Service with {handlerCount} OutputHandlerInfos ({activeCount} active) and {preprocessorCount} OutputPreprocessors",
            _handlerInfos.Count, _activeHandlers.Count, _preprocessors.Count);
    }

    public override void Stop()
    {
        var activeHandlerCount = _activeHandlers.Count;
        _logger.Debug("Stopping service, shutting down {activeHandlers} Handlers", activeHandlerCount);
        for (var i = _activeHandlers.Count - 1; i >= 0; i--)
        {
            var handler = _activeHandlers[i];
            ShutdownHandlerSafe(handler);
        }

        var stillActiveHandlers = _activeHandlers.Where(x => x.GetCurrentStatus() != ServiceStatus.Stopped).ToArray();
        if (stillActiveHandlers.Length > 0)
        {
            var notStoppedHandlers = string.Join(", ", stillActiveHandlers.Select(x => x.GetType().FullName));
            _logger.Error("Following Handlers failed to comply with a shutdown call: {notStoppedHandlers}", notStoppedHandlers);
            throw new StartStopServiceException($"Following Handlers failed to comply with a shutdown call: {notStoppedHandlers}");
        }
        _activeHandlers.Clear();
        _handlerInfos.Clear();
        _preprocessors.Clear();
        _logger.Debug("Stopped service, shut down {activeHandlers} Handlers", activeHandlerCount);
    }

    public override void Restart()
    {
        RestartSimple(GetType(), _logger);
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
            var infoMatch = _handlerInfos.FirstOrDefault(x => x.HandlerType == handlerType);
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
        var activeHandler = RetrieveActiveHandlerByType(handlerInfo.HandlerType);
        if (activeHandler is null) return ServiceStatus.Stopped;
        var activeStatus = activeHandler.GetCurrentStatus();
        if (activeStatus == ServiceStatus.Stopped)
        {
            _logger.Warning("GetHandlerStatus: Retrieved stopped Handler with type \"{handlerType}\" from active list",
                handlerInfo.HandlerType.FullName);
        }
        return activeStatus;
    }
    #endregion

    #region Handlers => Internal Start / Stop Unsafe
    private void ActivateHandlerUnsafe(IOutputHandlerStartInfo handlerInfo)
    {
        _logger.Information("Activating Handler with type \"{handlerType}\"", handlerInfo.HandlerType.FullName);
        var activeMatch = RetrieveActiveHandlerByType(handlerInfo.HandlerType);
        if (activeMatch is not null)
        {
            _logger.Debug("Terminating old Handler with type \"{handlerType}\"", handlerInfo.HandlerType.FullName);
            ShutdownHandlerUnsafe(activeMatch);

            activeMatch = RetrieveActiveHandlerByType(handlerInfo.HandlerType);
            if (activeMatch is null)
            {
                _logger.Debug("Terminated old Handler with type \"{handlerType}\"", handlerInfo.HandlerType.FullName);
            }
            else
            {
                _logger.Error("Failed to terminate old Handler with type \"{handlerType}\"", handlerInfo.HandlerType.FullName);
                throw new StartStopServiceException($"Unable to shut down Handler {handlerInfo.HandlerType.FullName}");
            }
        }

        var newHandler = RetrieveHandlerInstanceForType(handlerInfo.HandlerType);
        newHandler.OnRuntimeError += HandleOnRuntimeError;
        newHandler.OnSubmoduleStopped += HandleOnSubmoduleStopped;
        newHandler.Start();
        _activeHandlers.Add(newHandler);
        _logger.Information("Activated Handler with type \"{handlerType}\"", handlerInfo.HandlerType.FullName);
    }

    private void ShutdownHandlerUnsafe(IOutputHandler handler)
    {
        _logger.Information("Shutting down Handler with type \"{hanlderType}\"", handler.GetType().FullName);
        handler.Clear();
        handler.OnSubmoduleStopped -= HandleOnSubmoduleStopped; //This is not needed when manually shutting down
        handler.Stop();
        CleanupAfterHandlerShutdown(handler);
        _logger.Information("Shut down Handler with type \"{handlerType}\"", handler.GetType().FullName);
    }

    private void RestartHandlerUnsafe(IOutputHandler handler)
    {
        _logger.Information("Restarting Handler with type \"{handlerType}\"", handler.GetType().FullName);
        handler.OnSubmoduleStopped -= HandleOnSubmoduleStopped;
        handler.Restart();
        handler.OnSubmoduleStopped += HandleOnSubmoduleStopped;
        _logger.Information("Restarted Handler with type \"{handlerType}\"", handler.GetType().FullName);
    }
    #endregion

    #region Handlers => Internal Start / Stop Safe
    private bool ActivateHandlerSafe(IOutputHandlerStartInfo handlerInfo)
    {
        try
        {
            ActivateHandlerUnsafe(handlerInfo);
            return true;
        }
        catch (Exception ex)
        {
            AddRefreshException(ex, "Unable to safely activate handler with type \"{handlerType}\"", handlerInfo.HandlerType.FullName);
            return false;
        }
    }

    private bool RestartHandlerSafe(IOutputHandler handler)
    {
        try
        {
            RestartHandlerUnsafe(handler);
            return true;
        }
        catch (Exception e)
        {
            AddRefreshException(e, "Failed to restart handler with type \"{handlerType}\"", handler.GetType().FullName);
            return false;
        }
    }

    private bool ShutdownHandlerSafe(IOutputHandler handler)
    {
        try
        {
            ShutdownHandlerUnsafe(handler);
            return true;
        }
        catch (Exception ex)
        {
            AddRefreshException(ex, "Unable to safely shutdown handler with type \"{handlerType}\"", handler.GetType().FullName);
            return false;
        }
    }

    private void HandleOnSubmoduleStopped(object? sender, EventArgs e)
    {
        if (sender is null) return;
        _logger.Warning("HandleOnSubmoduleStopped called for type \"{senderType}\", this should only happen when a shutdown was called unexpectedly",
            sender.GetType().FullName);
        if (sender is not IOutputHandler handler) return;
        CleanupAfterHandlerShutdown(handler);
    }

    private void CleanupAfterHandlerShutdown(IOutputHandler handler)
    {
        handler.OnRuntimeError -= HandleOnRuntimeError;
        handler.OnSubmoduleStopped -= HandleOnSubmoduleStopped;
        _activeHandlers.Remove(handler);
    }
    #endregion

    #region Handlers => Public Control
    public void RefreshHandlers()
    {
        _refreshExceptions.Clear();

        var diagnosticSw = Stopwatch.StartNew();
        _logger.Debug("Refreshing Output Handlers...");
        List<Type> permittedTypes = [];

        //Disabling and enabling all handlers
        foreach(var handlerInfo in _handlerInfos)
        {
            var match = RetrieveActiveHandlerByType(handlerInfo.HandlerType);
            if (handlerInfo.ShouldBeEnabled())
            {
                if (match is null)
                {
                    _logger.Debug("Handler of type \"{handlerType}\" is enabled but not active, starting...",
                        handlerInfo.HandlerType);
                    ActivateHandlerSafe(handlerInfo);
                }
                permittedTypes.Add(handlerInfo.HandlerType);
            } else
            {
                if (match is not null)
                {
                    _logger.Debug("Handler of type \"{handlerType}\" is disabled but active, stopping...",
                        handlerInfo.HandlerType);
                    ShutdownHandlerSafe(match);
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
            var res = ShutdownHandlerSafe(handler);
            if (!res)
            {
                _logger.Warning("Handler of type \"{handlerType}\" failed shutting down, removing from list forcefully - This should not be ignored!",
                    handlerType);
                CleanupAfterHandlerShutdown(handler);
            }
        }

        diagnosticSw.Stop();
        _logger.Debug("Finished refreshing Output Handlers in {timeMs}ms", diagnosticSw.ElapsedMilliseconds);

        NotifyIfRefreshExceptions();
    }

    public void RestartHandlers() 
    {
        _refreshExceptions.Clear();

        _logger.Debug("Restarting all {handlerCount} active Handlers...", _activeHandlers.Count);
        foreach(var handler in _activeHandlers)
        {
            RestartHandlerSafe(handler);
        }
        _logger.Debug("Finished restarting all {handlerCount} active Handlers", _activeHandlers.Count);

        NotifyIfRefreshExceptions();
    }
    #endregion

    #region Handlers => Exception Handling
    private void HandleOnRuntimeError(object? sender, Exception ex)
    {
        var handlerType = sender?.GetType();
        _logger.Error(ex, "Encountered an error in Handler \"{handlerType}\"", handlerType?.FullName);
        _notify.SendError("Handler error", $"Encountered an error in Handler {handlerType?.FullName ?? "???"}", exception: ex);
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

    private IOutputHandler RetrieveHandlerInstanceForType(Type type)
    {
        var searchMatch = _loadOutputHandler.GetInstance(type);
        if (searchMatch is null)
        {
            _logger.Error("Unable to retrieve Handler for type \"{handlerType}\"", type.FullName);
            throw new DiResolveException($"Unable to retrieve Handler for type {type.FullName}");
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

    private bool IsHandlerCompatible(IOutputHandler handler, OutputSettingsFlags settings)
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

    public void SetProcessingIndicator(bool isProcessing)
    {
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

    private bool IsPreprocessorCompatible(IOutputPreprocessor preprocessor, OutputSettingsFlags settings)
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
    public override Exception? GetFaultIfExists()
    {
        var exList = new List<Exception>();

        var baseException = base.GetFaultIfExists();
        if (baseException is not null)
        {
            exList.Add(baseException);
        }
        exList.AddRange(_refreshExceptions);
        exList.AddRange(GetHandlerExceptions());

        return exList.Count == 0
            ? null
            : exList.Count > 1
                ? new CombinedException(exList)
                : exList[0];
    }

    private void AddRefreshException(Exception ex, string message, params object?[]? args)
    {
        _logger.Error(ex, message, args);
        _refreshExceptions.Add(ex);
    }

    public void NotifyIfRefreshExceptions()
    {
        if (_refreshExceptions.Count == 0) return;
        
        var ex = new CombinedException(_refreshExceptions);
        _logger.Warning(ex, "Following exceptions popped up during refresh");
        _notify.SendError("Errors ocurred during refresh", "The following errors occured while refreshing handlers", ex);
    }

    public List<Exception> GetHandlerExceptions()
    {
        var exList = new List<Exception>();
        foreach (var handler in _activeHandlers)
        {
            var ex = handler.GetFaultIfExists();
            if (ex is not null)
                exList.Add(ex);
        }
        return exList;
    }
    #endregion
}