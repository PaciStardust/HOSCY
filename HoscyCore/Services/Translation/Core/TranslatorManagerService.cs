using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Interfacing;
using Serilog;

namespace HoscyCore.Services.Translation.Core;

[PrototypeLoadIntoDiContainer(typeof(ITranslatorManagerService))]
public class TranslatorManagerService //todo: [TEST] Write tests for this
(
    IBackToFrontNotifyService notify,
    ILogger logger,
    ConfigModel config,
    IContainerBulkLoader<ITranslationProviderStartInfo> infoLoader,
    IContainerBulkLoader<ITranslationProvider> providerLoader
)
: StartStopServiceBase, ITranslatorManagerService
{
    #region Injected
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly ILogger _logger = logger.ForContext<TranslatorManagerService>();
    private readonly ConfigModel _config = config;
    private readonly IContainerBulkLoader<ITranslationProviderStartInfo> _infoLoader = infoLoader;
    private readonly IContainerBulkLoader<ITranslationProvider> _providerLoader = providerLoader;
    #endregion

    #region Service Vars
    private ITranslationProvider? _currentProvider;
    private readonly List<ITranslationProviderStartInfo> _providerInfos = [];
    private readonly List<Exception> _refreshExceptions = [];
    #endregion

    #region Info
    public IReadOnlyList<ITranslationProviderStartInfo> GetProviderInfos()
    {
        return _providerInfos;
    }

    public ITranslationProviderStartInfo? GetCurrentProviderInfo()
    {
        if (_currentProvider is null)
            return null;
        
        var searchType = _currentProvider.GetType();
        var returnType = SearchInfos(x => x.ProviderType == searchType, $"Type={searchType.Name}");

        if (returnType is null)
        {
            _notify.SendError("Error retrieving active provider", $"Unable to locate current provider ({searchType.Name}) in info list");
            _logger.Error("Unable to locate current provider ({provider}) in info list", searchType.FullName);
        }
        return returnType;
    }

    public ServiceStatus GetCurrentProviderStatus()
    {
        return _currentProvider?.GetCurrentStatus() ?? ServiceStatus.Stopped;
    }
    #endregion

    #region Start / Stop
    protected override void StartInternal()
    {
        _logger.Debug("Starting up Service - Loading available TranslationProviderStartInfos and perform provider refresh");
        if (IsStarted())
        {
            _logger.Debug("Skipped starting Service, still running");
            return;
        }

        _providerInfos.Clear();

        _providerInfos.AddRange(_infoLoader.GetInstances());
        if (_providerInfos.Count == 0)
        {
            _logger.Warning("No TranslationProviderStartupInfos could be located, Service will have no functionality and will be NOT be marked as running");
            return;
        }

        RefreshProvider();
        _logger.Debug("Started up Service with {providerCount} TranslationProviderStartInfos and provider refresh", _providerInfos.Count);
    }

    public override void Stop()
    {
        _logger.Debug("Stopping service, shutting down Provider");
        StopCurrentProvider();
        _providerInfos.Clear();
        _logger.Debug(messageTemplate: "Stopped service, shut down Provider");
    }

    public override void Restart()
    {
        RestartSimple(GetType(), _logger);
    }

    protected override bool IsStarted()
        => _providerInfos.Count > 0;
    protected override bool IsProcessing()
        => IsStarted() && _currentProvider is not null;
    #endregion

    #region Provider => Start / Stop
    public void RefreshProvider()
    {
        _logger.Information("Performing provider refresh");
        _refreshExceptions.Clear();

        var infoMatch = string.IsNullOrWhiteSpace(_config.Translation_CurrentProvider)
            ? null 
            : SearchInfos(x => x.Name.Equals(_config.Translation_CurrentProvider, StringComparison.OrdinalIgnoreCase),
                $"Name={_config.Translation_CurrentProvider}");

        if (_currentProvider is not null)
        {
            var currentType = _currentProvider.GetType();
            if (infoMatch is not null && infoMatch.ProviderType == currentType)
            {
                _logger.Information("Performed provider refresh, no changes needed");
                return;
            }

            _logger.Debug("Current provider {currentProvider} does not match selected type {selectedType}, stopping",
                currentType.Name, infoMatch?.ProviderType.Name ?? "[EMPTY]");
            StopCurrentProvider();
        }

        if (infoMatch is not null)
        {
            _logger.Debug("Starting new provider with name {providerName} and type {providerType}",
                infoMatch.Name, infoMatch.ProviderType.Name);
            StartProvider(infoMatch);
        }

        NotifyIfRefreshExceptions();
        _logger.Information("Performed provider refresh");
    }

    private bool StartProvider(ITranslationProviderStartInfo startInfo)
    {
        try
        {
            _logger.Information("Attemtping to start provider with name \"{name}\" and typeName \"{typeName}\"", startInfo.Name, startInfo.ProviderType.Name);
            if (_currentProvider is not null)
            {
                _logger.Information("Skipped starting Provider, still running as \"{providerType}\"", _currentProvider.GetType().FullName);
                return true;
            }

            var providerMatch = _providerLoader.GetInstance(startInfo.ProviderType);
            if (providerMatch is null)
            {
                _logger.Error("Failed to get provider instance name \"{providerName}\" and type \"{providerType}\"", startInfo.Name, startInfo.ProviderType.Name);
                throw new DiResolveException($"Failed to get provider instance name {startInfo.Name} and type {startInfo.ProviderType.Name}");
            }

            providerMatch.OnRuntimeError += HandleOnRuntimeError;
            providerMatch.OnSubmoduleStopped += HandleOnSubmoduleStopped;
            providerMatch.Start();
            _currentProvider = providerMatch;
            _logger.Information("Started provider with name \"{providerName}\" and type \"{providerType}\"", startInfo.Name, startInfo.ProviderType.Name);
        }
        catch (Exception ex)
        {
            AddRefreshException(ex, "Failed to start provider with name \"{name}\" and type \"{typeName}\"",
                startInfo.Name, startInfo.ProviderType.Name);
            return false;
        }
        return true;
    }

    private bool StopCurrentProvider()
    {
        try
        {
            _logger.Information("Stopping current provider");
            if (_currentProvider is null)
            {
                _logger.Information("Skipping stopping of current provider, no provider running");
                return true;
            }
            _currentProvider.OnSubmoduleStopped -= HandleOnSubmoduleStopped;
            _currentProvider.Stop();
            CleanupAfterProviderShutdown();
            _logger.Information("Stopped current provider");
        }
        catch (Exception ex)
        {
            AddRefreshException(ex, "Failed to stop current provider");
            return false;
        }
        return true;
    }

    public bool RestartCurrentProvider()
    {
        _logger.Information("Restarting current provider");
        _refreshExceptions.Clear();
        try
        {
            if (_currentProvider is null)
            {
                _logger.Information("Skipping restart of current provider, no provider running");
                return true;
            }
            _currentProvider.OnSubmoduleStopped -= HandleOnSubmoduleStopped;
            _currentProvider.Restart();
            _currentProvider.OnSubmoduleStopped += HandleOnSubmoduleStopped;
            _logger.Information("Restarted current provider");
        } catch (Exception ex)
        {
            AddRefreshException(ex,"Failed to restart current provider");
            NotifyIfRefreshExceptions();
            return false;
        }
        return true;
    }

    private void HandleOnSubmoduleStopped(object? sender, EventArgs e)
    {
        if (sender is null) return;
        _logger.Warning("HandleOnShutdownCompleted called for provider of type \"{senderType}\", this should only happen when a shutdown was called unexpectedly", sender.GetType().FullName);
        CleanupAfterProviderShutdown();
    }

    private void CleanupAfterProviderShutdown()
    {
        if (_currentProvider is not null)
        {
            _currentProvider.OnRuntimeError -= HandleOnRuntimeError;
            _currentProvider.OnSubmoduleStopped -= HandleOnSubmoduleStopped;
            _currentProvider = null;
        }
    }

    private void HandleOnRuntimeError(object? sender, Exception ex)
    {
        var providerType = sender?.GetType();
        _logger.Error(ex, "Encountered an error in Provider \"{providerType}\"", providerType?.FullName);
        _notify.SendError("Provider error", $"Encountered an error in Provider {providerType?.FullName ?? "???"}",exception: ex);
    }
    #endregion

    #region Provider => Functionality
    private readonly char[] _filterChars = ['\n', '\t', '\r', ' '];
    public TranslationResult TryTranslate(string input, out string? output)
    {
        if (_currentProvider is null || _currentProvider.GetCurrentStatus() == ServiceStatus.Stopped)
        {
            LogProviderNotAvailable(input);
            output = null;
            return _config.Translation_SendUntranslatedIfFailed 
                ? TranslationResult.UseOriginal
                : TranslationResult.Failed;
        }

        if (input.Length > _config.Translation_MaxTextLength)
        {
            if (_config.Translation_SkipLongerMessages)
            {
                _logger.Debug("Skipping translation and handling of message with contents \"{contents}\" as skipping of messages longer than {charLimit} characters is enabled",
                    input, _config.Translation_MaxTextLength);
                output = null;
                return _config.Translation_SendUntranslatedIfFailed 
                    ? TranslationResult.UseOriginal
                    : TranslationResult.Failed;
            }

            var spaceLocated = false;
            for (var i = _config.Translation_MaxTextLength; i > -1; i--)
            {
                if (_filterChars.Contains(input[i]))
                {
                    spaceLocated = true;
                }
                else
                {
                    if (!spaceLocated) continue;
                    input = i > 0
                        ? input[..i]
                        : input[.._config.Translation_MaxTextLength]; //todo: [TEST] Does this crop correctly and do return values work?
                    break;
                }
            }
        }

        var result = _currentProvider.TryTranslate(input, out var translatedOutput);
        switch (result)
        {
            case TranslationResult.Succeeded:
                output = translatedOutput;
                return result;
            case TranslationResult.Failed:
                output = null;
                LogFailedTranslation(input);
                return _config.Translation_SendUntranslatedIfFailed
                    ? TranslationResult.UseOriginal
                    : TranslationResult.Failed;
            case TranslationResult.UseOriginal:
                output = null;
                return result;
            default:
                _logger.Warning("Unexpected translation result {result} when translating \"{input}\"",
                    result, input);
                output = null;
                return TranslationResult.Failed;
        }
    }

    private void LogProviderNotAvailable(string inputForLog)
    {
        _logger.Warning("Skipped translation request for input \"{input}\", no provider running", inputForLog);
    }

    private void LogFailedTranslation(string inputForLog)
    {
        _logger.Warning("Translation of message with contents \"{input}\" failed", inputForLog);
    }
    #endregion

    #region Utils
    private ITranslationProviderStartInfo? SearchInfos(Predicate<ITranslationProviderStartInfo> search, string searchInfoForLog)
    {
        var infoSearch = _providerInfos.Where(search.Invoke).ToArray();

        if (infoSearch.Length == 0)
        {
            _logger.Error("No info found with search info {searchInfo}", searchInfoForLog);
            return null;
        }

        if (infoSearch.Length > 1)
        {
            var translatorOptions = string.Join(", ", infoSearch.Select(x => $"{x.Name} ({x.ProviderType.Name})"));
            _logger.Warning("Multiple infos found with search info {searchInfo}: {translatorOptions} => picking first", searchInfoForLog);
        }

        return infoSearch[0];
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
        var providerError = _currentProvider?.GetFaultIfExists();
        if (providerError is not null)
        {
            exList.Add(providerError);
        }
        exList.AddRange(_refreshExceptions);

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
    #endregion
}