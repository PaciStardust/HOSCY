using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Network;
using HoscyCore.Services.Translation.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Translation.Modules;

[LoadIntoDiContainer(typeof(ApiTranslationModuleStartInfo))]
public class ApiTranslationModuleStartInfo : ITranslationModuleStartInfo
{
    public string Name => "Api Translator";
    public string Description => "Translation using any provided API Info";
    public Type ModuleType => typeof(ApiTranslationModule);

    public TranslationModuleConfigFlags ConfigFlags
        => TranslationModuleConfigFlags.Api;
}

[PrototypeLoadIntoDiContainer(typeof(ApiTranslationModule), Lifetime.Transient)]
public class ApiTranslationModule(ILogger logger, ConfigModel config, IApiClient client)
    : TranslationModuleBase(logger.ForContext<ApiTranslationModule>())
{
    private readonly ConfigModel _config = config;
    private readonly IApiClient _client = client.AddIdentifier(nameof(ApiTranslationModule));

    #region Start/Stop
    protected override Res StartForService()
    {
        _logger.Debug("Starting Translator with preset \"{preset}\"", _config.Translation_Api_Preset);
        var matchingModel = _config.Api_Presets.FirstOrDefault(x => x.Name == _config.Translation_Api_Preset);
        if (matchingModel is null)
            return ResC.FailLog($"Could not find api preset {_config.Translation_Api_Preset}", _logger);

        var loaded = _client.LoadPreset(matchingModel);
        if (!loaded.IsOk) return loaded;
        
        _logger.Debug("Started Translator with preset \"{preset}\"", matchingModel.Name);
        return loaded;
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForModule()
    {
        if (_client.IsPresetLoaded())
        {
            _logger.Debug("Stopping Translator if needed");
            _client.ClearPreset();
        }
        return ResC.Ok();
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
        var result = ResC.TWrap(() => _client.SendTextAsync(input).GetAwaiter().GetResult(),
            "Failed translation of text \"{input}\" via exception", _logger);

        if (!result.IsOk)
        {
            _logger.Warning("Failed translation of text \"{input}\" ({result})", result);
            output = null;
            SetFault(new Exception(result.Msg.Message));
            return TranslationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(result.Value))
        {
            _logger.Warning("Failed translation of text \"{input}\", no output received", input);
            output = null;
            return TranslationResult.Failed;
        }

        _logger.Verbose("Translated text \"{input}\" to \"{output}\"", input, result);
        output = result.Value;
        return TranslationResult.Succeeded;
    }
    #endregion
}