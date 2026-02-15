using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Network;
using HoscyCore.Services.Translation.Core;
using Serilog;

namespace HoscyCore.Services.Translation.Providers;

[LoadIntoDiContainer(typeof(ApiTranslationProviderStartInfo))]
public class ApiTranslationProviderStartInfo : ITranslationProviderStartInfo
{
    public string Name => "Api Translator";
    public string Description => "Translation using any provided API Info";
    public Type ProviderType => typeof(ApiTranslationProvider);
}

[PrototypeLoadIntoDiContainer(typeof(ApiTranslationProvider), Lifetime.Transient)] //todo: [TEST] Write tests for this
public class ApiTranslationProvider(ILogger logger, ConfigModel config, IApiClient client) : TranslationProviderBase
{
    private readonly ILogger _logger = logger.ForContext<ApiTranslationProvider>();
    private readonly ConfigModel _config = config;
    private readonly IApiClient _client = client.AddIdentifier(nameof(ApiTranslationProvider));

    #region Start/Stop
    protected override void StartInternal()
    {
        _logger.Debug("Starting Translator with preset \"{preset}\"", _config.Translation_Api_Preset);
        var matchingModel = _config.Api_Presets.FirstOrDefault(x => x.Name == _config.Translation_Api_Preset);
        if (matchingModel is null)
        {
            _logger.Error("Could not find preset \"{preset}\"", _config.Translation_Api_Preset);
            throw new StartStopServiceException($"Could not find preset {_config.Translation_Api_Preset}");
        }

        var loaded = _client.LoadPreset(matchingModel);
        if (!loaded)
        {
            _logger.Error("Could not find load \"{preset}\"", matchingModel.Name);
            throw new StartStopServiceException($"Could not load preset {matchingModel.Name}, check logs for more information");
        }
        _logger.Debug("Started Translator with preset \"{preset}\"", matchingModel.Name);
    }

    protected override void StopInternal()
    {
        _logger.Debug("Stopping Translator");
        if (_client.IsPresetLoaded())
        {
            _client.ClearPreset();
        }
        _logger.Debug("Stopped Translator");
    }

    public override void Restart()
    {
        RestartSimple(GetType(), _logger);
    }

    protected override bool IsStarted()
        => _client.IsPresetLoaded();
    protected override bool IsProcessing()
        => IsStarted();
    #endregion

    #region Functionality
    public override TranslationResult TryTranslate(string input, out string? output)
    {
        input = input.Replace("\"", string.Empty);

        if (string.IsNullOrWhiteSpace(input))
        {
            output = null;
            return TranslationResult.Failed;
        }

        _logger.Verbose("Requesting translation of text \"{input}\"", input);
        try
        {
            var result = _client.SendTextAsync(input).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.Warning("Failed translation of text \"{input}\", no output received", input);
                output = null;
                return TranslationResult.Failed;
            }

            _logger.Verbose("Translated text \"{input}\" to \"{output}\"", input, result);
            output = result;
            return TranslationResult.Succeeded;
        } catch (Exception ex)
        {
            _logger.Error(ex, "Failed translation of text \"{input}\"", input);
            SetFault(ex);
            output = null;
            return TranslationResult.Failed;
        }
    }
    #endregion
}