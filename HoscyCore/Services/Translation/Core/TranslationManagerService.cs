using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using Serilog;

namespace HoscyCore.Services.Translation.Core;

[PrototypeLoadIntoDiContainer(typeof(ITranslationManagerService))]
public class TranslationManagerService
(
    ConfigModel config,
    IBackToFrontNotifyService notify,
    ILogger logger,
    IContainerBulkLoader<ITranslationModuleStartInfo> infoLoader,
    IContainerBulkLoader<ITranslationModule> moduleLoader
)
    : StartStopModuleControllerBase<ITranslationModuleStartInfo, ITranslationModule>
        (notify, logger, infoLoader, moduleLoader),
    ITranslationManagerService
{

    #region Injected
    private readonly ConfigModel _config = config;
    #endregion

    #region Module => Functionality
    protected override string GetSelectedModuleName()
        => _config.Translation_CurrentModule;

    private readonly char[] _filterChars = ['\n', '\t', '\r', ' '];

    public TranslationResult TryTranslate(string input, out string? output)
    {
        if (_currentModule is null || _currentModule.GetCurrentStatus() == ServiceStatus.Stopped)
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

            var spaceIndex = -1;
            for (var i = _config.Translation_MaxTextLength - 1; i > -1; i--)
            {
                if (_filterChars.Contains(input[i]))
                {
                    spaceIndex = i;
                    break;
                }
            }
            input = (spaceIndex > -1
                ? input[..spaceIndex]
                : input[.._config.Translation_MaxTextLength])
                .TrimEnd();
        }

        var result = _currentModule.TryTranslate(input, out var translatedOutput);
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
}