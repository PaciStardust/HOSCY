using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Translation.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Output.Core;

[LoadIntoDiContainer(typeof(IOutputManagerService), Lifetime.Singleton)]
public class OutputManagerService(ILogger logger, IServiceProvider services, IBackToFrontNotifyService notify, ITranslatorManagerService translator, ConfigModel config) : StartStopServiceBase, IOutputManagerService
{
    #region Injected
    private readonly ILogger _logger = logger.ForContext<OutputManagerService>();
    private readonly IServiceProvider _services = services;
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly ITranslatorManagerService _translator = translator;
    private readonly ConfigModel _config = config;
    #endregion

    #region Service Vars
    private readonly List<OutputProcessorInfo> _availableProcessors = [];
    private readonly List<IOutputProcessor> _activeProcessors = [];
    private readonly List<IOutputPreprocessor> _preprocessors = [];
    #endregion

    #region Events
    public event EventHandler<(string, string?)> OnMessage = delegate { };
    public event EventHandler<OutputNotificationEventArgs> OnNotification = delegate {};
    public event EventHandler OnClear = delegate {};
    public event EventHandler<bool> OnProcessingIndicatorSet = delegate {};
    #endregion

    #region Start / Stop
    protected override void StartInternal() //todo: automatic starting of processors?
    {
        _logger.Information("Starting up Service by loading available OutputProcessors");
        if (IsStarted())
        {
            _logger.Information("Skipped starting Service, still running");
            return;
        }

        _availableProcessors.Clear();
        _preprocessors.Clear();

        var processorsWithInstance = LaunchUtils.GetImplementationsInContainerForClass<IOutputProcessor>(_services, _logger);
        _availableProcessors.AddRange(processorsWithInstance.Select(x => x.GetIdentifier()));
        if (_availableProcessors.Count == 0)
        {
            _logger.Warning("No Output Processors could be located, Service will have no functionality and will be NOT be marked as running");
            return;
        }

        _logger.Information("Loading Preprocessors");
        var preprocessorsWithInstance = LaunchUtils.GetImplementationsInContainerForClass<IOutputPreprocessor>(_services, _logger);
        _preprocessors.AddRange(preprocessorsWithInstance.OrderBy(x => x.GetHandlingStage()));

        _logger.Information("Started up Service with {processorCount} OutputProcessors and {preprocessorCount} OutputPreprocessors", _availableProcessors.Count, _preprocessors.Count);
    }

    protected override bool IsStarted()
        => _preprocessors.Count > 0 || _availableProcessors.Count > 0;
    protected override bool IsProcessing()
        => IsStarted() || _activeProcessors.Count > 0;

    public override void Stop()
    {
        var activeProcessorCount = _activeProcessors.Count;
        _logger.Information("Stopping service, shutting down {activeProcessors} Processors", activeProcessorCount);
        foreach (var processor in _activeProcessors)
        {
            ShutdownProcessor(processor.GetIdentifier());
        }

        var stillActiveProcessors = _activeProcessors.Where(x => x.GetCurrentStatus() != ServiceStatus.Stopped).ToArray();
        if (stillActiveProcessors.Length > 0)
        {
            var notStoppedProcessors = string.Join(", ", stillActiveProcessors.Select(x => x.GetType().FullName));
            _logger.Error("Following MessageProcessors failed to comply with a shutdown call: {notStoppedProcessors}", notStoppedProcessors);
            throw new StartStopServiceException($"Following MessageProcessors failed to comply with a shutdown call: {notStoppedProcessors}");
        }
        _activeProcessors.Clear();
        _logger.Information("Stopped service, shut down {activeProcessors} Processors", activeProcessorCount);
    }

    public override void Restart()
    {
        RestartSimple(GetType(), _logger);
    }
    #endregion

    #region Info
    public IReadOnlyList<OutputProcessorInfo> GetInfos(bool activeOnly)
    {
        return activeOnly
            ? _activeProcessors.Where(x => x.GetCurrentStatus() != ServiceStatus.Stopped).Select(x => x.GetIdentifier()).ToList()
            : _availableProcessors;
    }
    #endregion

    #region Processor => Start / Stop
    public void ActivateProcessor(OutputProcessorInfo info)
    {
        _logger.Information("Activating Processor with name \"{processorName}\" and type \"{processorType}\"", info.Name, info.GetType().FullName);
        var activeMatch = RetrieveActiveProcessorWithInfo(info);
        if (activeMatch is not null)
        {
            _logger.Information("Terminating old Processor with name \"{processorName}\" and type \"{processorType}\"", info.Name, info.GetType().FullName);
            ShutdownProcessor(info);
        }
        activeMatch = RetrieveActiveProcessorWithInfo(info);
        if (activeMatch is null)
        {
            _logger.Information("Terminated old Processor with name \"{processorName}\" and type \"{processorType}\"", info.Name, info.GetType().FullName);
        }
        else
        {
            _logger.Error("Failed to terminate old Processor with name \"{processorName}\" and type \"{processorType}\"", info.Name, info.GetType().FullName);
            throw new StartStopServiceException($"Unable to shut down Processor {info.ProcessorType.FullName}");
        }

        SetFault(GetProcessorExceptions());
        var newProcessor = RetrieveProcessorInstanceWithInfo(info);
        newProcessor.OnRuntimeError += HandleOnRuntimeError;
        newProcessor.OnSubmoduleStopped += HandleOnSubmoduleStopped;
        newProcessor.Start();
        _activeProcessors.Add(newProcessor);
        _logger.Information("Activated Processor with name \"{processorName}\" and type \"{processorType}\"", info.Name, info.GetType().FullName);
    }

    public ServiceStatus GetProcessorStatus(OutputProcessorInfo info)
    {
        var activeProcessor = RetrieveActiveProcessorWithInfo(info);
        if (activeProcessor is null) return ServiceStatus.Stopped;
        var activeStatus = activeProcessor.GetCurrentStatus();
        if (activeStatus == ServiceStatus.Stopped)
        {
            _logger.Warning("GetProcessorStatus: Retrieved stopped Processor with name \"{processorName}\" and type \"{processorType}\" from active list",
                info.Name, info.ProcessorType.FullName);
        }
        return activeStatus;
    }

    public void ShutdownProcessor(OutputProcessorInfo info)
    {
        _logger.Information("Shutting down Processor with name \"{processorName}\" and type \"{processorType}\"", info.Name, info.GetType().FullName);
        var activeProcessor = RetrieveActiveProcessorWithInfo(info);
        if (activeProcessor is null)
        {
            _logger.Information("Processor with name \"{processorName}\" and type \"{processorType}\" is not active or does not exist", info.Name, info.GetType().FullName);
            return;
        }

        activeProcessor.Clear();
        activeProcessor.OnSubmoduleStopped -= HandleOnSubmoduleStopped; //This is not needed when manually shutting down
        activeProcessor.Stop();
        CleanupAfterProcessorShutdown(activeProcessor);
        _logger.Information("Shut down Processor with name \"{processorName}\" and type \"{processorType}\"", info.Name, info.GetType().FullName);
    }

    private void HandleOnSubmoduleStopped(object? sender, EventArgs e)
    {
        if (sender is null) return;
        _logger.Warning("HandleOnShutdownCompleted called for type \"{senderType}\", this should only happen when a shutdown was called unexpectedly", sender.GetType().FullName);
        if (sender is not IOutputProcessor processor) return;
        CleanupAfterProcessorShutdown(processor);
    }

    private void CleanupAfterProcessorShutdown(IOutputProcessor processor)
    {
        processor.OnRuntimeError -= HandleOnRuntimeError;
        processor.OnSubmoduleStopped -= HandleOnSubmoduleStopped;
        _activeProcessors.Remove(processor); //todo: TEST => does that work?
        SetFault(GetProcessorExceptions());
    }

    public void RestartProcessor(OutputProcessorInfo info)
    {
        _logger.Information("Restarting Processor with name \"{processorName}\" and type \"{processorType}\"", info.Name, info.GetType().FullName);
        var activeProcessor = RetrieveActiveProcessorWithInfo(info);
        if (activeProcessor is null)
        {
            _logger.Information("Could not fine active Processor with name \"{processorName}\" and type \"{processorType}\", starting instead", info.Name, info.GetType().FullName);
            ActivateProcessor(info);
        }
        else
        {
            activeProcessor.OnSubmoduleStopped -= HandleOnSubmoduleStopped;
            activeProcessor.Restart();
            activeProcessor.OnSubmoduleStopped += HandleOnSubmoduleStopped;
        }
        _logger.Information("Restarted Processor with name \"{processorName}\" and type \"{processorType}\"", info.Name, info.GetType().FullName);
    }

    private void HandleOnRuntimeError(object? sender, Exception ex)
    {
        _logger.Error(ex, "Encountered an error in Message Processor \"{senderType}\"", sender?.GetType().FullName);
        _notify.SendError($"Encountered an error in Message Processor {sender?.GetType().FullName ?? "???"}",exception: ex);
        var newCollectiveException = GetProcessorExceptions();
        if (ex is null)
        {
            _logger.Information("Clear OutputProcessorListException for Service");
        }
        else
        {
            _logger.Information(ex, "Set new OutputProcessorListException for Service");
        }
        SetFault(ex);
    }

    private OutputProcessorListException? GetProcessorExceptions()
    {
        var processorExceptions = new List<Exception>();
        foreach (var processor in _activeProcessors)
        {
            var processorException = processor.GetFaultIfExists();
            if (processorException is not null)
            {
                processorExceptions.Add(processorException);
            }
        }
        return new OutputProcessorListException(processorExceptions);
    }

    private IOutputProcessor? RetrieveActiveProcessorWithInfo(OutputProcessorInfo info)
    {
        var activeMatches = _activeProcessors.Where(x => x.GetIdentifier().ProcessorType == info.ProcessorType).ToArray();
        switch (activeMatches.Length)
        {
            case 0:
                return null;
            case 1:
                if (activeMatches[0].GetCurrentStatus() == ServiceStatus.Stopped)
                {
                    _logger.Warning("Processor with name \"{processorName}\" and type \"{processorType}\" was retrieved from active list despite being marked as stopped", info.Name, info.GetType().FullName);
                }
                return activeMatches[0];
            default:
                if (activeMatches.Any(x => x.GetCurrentStatus() == ServiceStatus.Stopped))
                {
                    _logger.Warning("One or multiple processors retrieved from active list are marked as stopped");
                }
                _logger.Warning("Found multiple active {procCount} processors for InfoType \"{infoType}\"", activeMatches.Length, info.ProcessorType.FullName);
                return activeMatches[0];
        }
    }

    private IOutputProcessor RetrieveProcessorInstanceWithInfo(OutputProcessorInfo info)
    {
        var availableMatches = _availableProcessors.Where(x => x.ProcessorType == info.ProcessorType).ToArray();

        switch (availableMatches.Length)
        {
            case 0:
                _logger.Warning("Could not find any available processors for InfoType \"{infoType}\"", info.ProcessorType.FullName);
                throw new ArgumentException($"Could not find any available processors for InfoType {info.ProcessorType.FullName}");
            case 1:
                break;
            default:
                _logger.Warning("Found multiple {procCount} processors for InfoType \"{infoType}\"", availableMatches.Length, info.ProcessorType.FullName);
                break;
        }

        if (_services.GetService(availableMatches[0].ProcessorType) is not IOutputProcessor searchMatch)
        {
            _logger.Error("Unable to retrieve Processor \"{processorName}\"", info.ProcessorType.FullName);
            throw new DiResolveException($"Unable to retrieve Processor {info.ProcessorType.FullName}");
        }
        return searchMatch;
    }
    #endregion

    #region Processor => Control
    public void SendMessage(string contents, OutputSettingsFlags settings)
    {
        if (string.IsNullOrWhiteSpace(contents)) return;

        var compatibleProcessors = _activeProcessors
            .Where(x => IsProcessorCompatible(x, settings))
            .ToArray();
        if (compatibleProcessors.Length == 0)
        {
            _logger.Warning("Message \"{message}\" was not processed as no processors fit the criteria", contents);
            return;
        }

        if (!settings.HasFlag(OutputSettingsFlags.DoNotPreprocess) 
            && TryPreprocess(contents, out var processedOutput))
        {
            if (string.IsNullOrWhiteSpace(processedOutput)) return;
            contents = processedOutput;
        }

        if (!settings.HasFlag(OutputSettingsFlags.DoNotTranslate) 
            && TryTranslateContentsIfNeeded(contents, compatibleProcessors, out var translatedText))
        {
            if (translatedText is null) return;
            SendMessageTranslatedInternal(contents, translatedText, compatibleProcessors);
        } 
        else
        {
            SendMessageInternal(contents, compatibleProcessors);
        }
    }

    private bool IsProcessorCompatible(IOutputProcessor processor, OutputSettingsFlags settings) //todo: Does this work?
    {
        var id = processor.GetIdentifier();
        var isNotCompatible = (settings.HasFlag(OutputSettingsFlags.SkipProcessorsWithTextOutput) && id.Flags.HasFlag(OutputProcessorInfoFlags.OutputAsText))
            || (settings.HasFlag(OutputSettingsFlags.SkipProcessorsWithOtherOutput) && id.Flags.HasFlag(OutputProcessorInfoFlags.OutputAsOther))
            || (settings.HasFlag(OutputSettingsFlags.SkipProcessorsWithAudioOutput) && id.Flags.HasFlag(OutputProcessorInfoFlags.OutputAsAudio));
        return !isNotCompatible;
    }

    private void SendMessageInternal(string contents, IOutputProcessor[] processors)
    {
        _logger.Debug("Sending {processorCount} processors a message with contents \"{contentsMessage}\"", processors.Length, contents);
        OnMessage.Invoke(this, (contents, null));
        foreach (var processor in processors)
        {
            processor.ProcessMessage(contents);
        }
        _logger.Debug("Sent {processorCount} processors a message with contents \"{contentsMessage}\"", processors.Length, contents);
}

    private void SendMessageTranslatedInternal(string contents, string translation, IOutputProcessor[] processors)
    {
        _logger.Debug("Sending {processorCount} processors a message with contents \"{contentsMessage}\" and translation \"{translation}\"", processors.Length, contents, translation);
        OnMessage.Invoke(this, (contents, translation));
        foreach (var processor in processors)
        {
            var newContents = processor.GetTranslationOutputMode() switch
            {
                TranslationOutputMode.Translation => translation,
                TranslationOutputMode.Untranslated => contents,
                TranslationOutputMode.Both => _config.ApiCommunication_Translation_AppendOriginal
                    ? $"{translation} / {contents}"
                    : translation,
                _ => throw new ArgumentException("Unsupported TranslationOutputMode")
            };
            processor.ProcessMessage(newContents);
        }
        _logger.Debug("Sent {processorCount} processors a message with contents \"{contentsMessage}\" and translation \"{translation}\"", processors.Length, contents, translation);
    }

    public void SendNotification(string contents, OutputNotificationPriority priority) //todo: should this not also respect output rules?
    {
        if (string.IsNullOrWhiteSpace(contents)) return;
        if (TryPreprocess(contents, out var processedOutput)) //todo: since when are these preprocessed?
        {
            if (string.IsNullOrWhiteSpace(processedOutput)) return;
            contents = processedOutput;
        }

        _logger.Debug("Sending {processorCount} processors a notification of priority {priority} with contents \"{contentsNotification}\"", _activeProcessors.Count, priority.ToString(), contents);
        OnNotification.Invoke(this, new OutputNotificationEventArgs(contents, priority));
        foreach (var processor in _activeProcessors)
        {
            processor.ProcessNotification(contents, priority);
        }
        _logger.Debug("Sent {processorCount} processors a notification of priority {priority} with contents \"{contentsNotification}\"", _activeProcessors.Count, priority.ToString(), contents);
    }

    public void Clear()
    {
        _logger.Debug("Sending {processorCount} processors a clear command", _activeProcessors.Count);
        OnClear(this, EventArgs.Empty);
        foreach (var processor in _activeProcessors)
        {
            processor.Clear();
        }
        _logger.Debug("Sent {processorCount} processors a clear command", _activeProcessors.Count);
    }

    public void SetProcessingIndicator(bool isProcessing)
    {
        _logger.Debug("Sending {processorCount} processors command to set processing indicator to {indicatorState}", _activeProcessors.Count, isProcessing);
        OnProcessingIndicatorSet(this, isProcessing);
        foreach (var processor in _activeProcessors)
        {
            processor.Clear();
        }
        _logger.Debug("Sent {processorCount} processors command to set processing indicator to {indicatorState}", _activeProcessors.Count, isProcessing);
    }

    private readonly char[] _filterChars = ['\n', '\t', '\r', ' '];
    /// <summary>
    /// Tries to translate if needed
    /// </summary>
    /// <param name="contents">Text to translate</param>
    /// <param name="translatedText">Translated text if sucessfully translated, null if returning false or an error occured</param>
    /// <returns>Attempted translation?</returns>
    private bool TryTranslateContentsIfNeeded(string contents, IOutputProcessor[] processors, out string? translatedText) //todo: fallback option?
    {
        if (!processors.Any(x => x.GetTranslationOutputMode() != TranslationOutputMode.Untranslated))
        {
            translatedText = null; //todo: ???
            return false;
        }

        if (contents.Length > _config.ApiCommunication_Translation_MaxTextLength)
        {
            if (_config.ApiCommunication_Translation_SkipLongerMessages)
            {
                _logger.Debug("Skipping translation and processing of message with contents \"{contents}\" as skipping of messages longer than {charLimit} characters is enabled",
                    contents, _config.ApiCommunication_Translation_MaxTextLength);
                translatedText = null;
                return true;
            }

            var spaceLocated = false;
            for (var i = _config.ApiCommunication_Translation_MaxTextLength; i > -1; i--)
            {
                if (_filterChars.Contains(contents[i]))
                {
                    spaceLocated = true;
                }
                else
                {
                    if (!spaceLocated) continue;
                    contents = i > 0
                        ? contents[..i]
                        : contents[.._config.ApiCommunication_Translation_MaxTextLength]; //todo: this correct?
                    break;
                }
            }
        }

        if (!_translator.TryTranslate(contents, out translatedText))
        {
            _logger.Warning("Skipping processing of message with contents \"{contents}\" as translation failed", contents);
            return false;
        }
        return true;
    }
    #endregion

    #region Preprocessors
    /// <summary>
    /// Tries using preprocessors on text
    /// </summary>
    /// <param name="input">String to preprocess</param>
    /// <param name="output">Preprocessed string if success and not handled entirely by a processor</param>
    /// <returns>Success</returns>
    private bool TryPreprocess(string input, out string? output) //todo: config values
    {
        _logger.Debug("Preprocessing \"{preProcessorInput}\" ...", input);
        string? currentOutput = null;
        foreach (var preprocessor in _preprocessors)
        {
            if (!preprocessor.TryProcess(currentOutput ?? input, out var processedOutput)) continue;

            if (!preprocessor.ShouldContinueIfHandled())
            {
                _logger.Debug("Preprocessor \"{preprocessorName}\" has done final handling on \"{preProcessorInput}\" with message \"{preProcessorOutput}\"", preprocessor.GetType().Name, input, processedOutput);
                output = null;
                return true;
            }

            _logger.Debug("Preprocessor \"{preprocessorName}\" converted \"{currentInput}\" to \"{currentOutput}\"", currentOutput ?? input, processedOutput);
            currentOutput = processedOutput;
        }
        output = currentOutput;
        return output is not null;
    }
    #endregion
}