using System.Diagnostics.CodeAnalysis;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Network;
using HoscyCore.Services.Translation.Core;
using Serilog;

namespace HoscyCore.Services.Translation.Translators;

public class ApiTranslator(ILogger logger, ConfigModel config, IApiClient client) : TranslatorBase
{
    private readonly ILogger _logger = logger.ForContext<ApiTranslator>();
    private readonly ConfigModel _config = config;
    private readonly IApiClient _client = client.AddIdentifier(nameof(ApiTranslator));

    #region Info
    public override string GetIdentifier()
        => "Api Translator";
    #endregion

    #region Start/Stop
    protected override void StartInternal()
    {
        _logger.Information("Starting Translator with preset {preset}", _config.ApiCommunication_Translation_CurrentPreset);
        var matchingModel = _config.ApiCommunication_Presets.FirstOrDefault(x => x.Name == _config.ApiCommunication_Translation_CurrentPreset);
        if (matchingModel is null)
        {
            _logger.Error("Could not find preset {preset}", _config.ApiCommunication_Translation_CurrentPreset);
            throw new StartStopServiceException($"Could not find preset {_config.ApiCommunication_Translation_CurrentPreset}");
        }

        var loaded = _client.LoadPreset(matchingModel);
        if (!loaded)
        {
            _logger.Error("Could not find load {preset}", matchingModel.Name);
            throw new StartStopServiceException($"Could not load preset {matchingModel.Name}, check logs for more information");
        }
        _logger.Information("Started Translator with preset {preset}", matchingModel.Name);
    }

    protected override void StopInternal()
    {
        _logger.Information("Stopping Translator");
        if (_client.IsPresetLoaded())
        {
            _client.ClearPreset();
        }
        _logger.Information("Stopped Translator");
    }

    public override void Restart()
    {
        RestartSimple(GetType(), _logger);
    }

    public override bool IsRunning()
    {
        return _client.IsPresetLoaded();
    }
    #endregion

    #region Functionality
    public override bool TryTranslate(string input, [NotNullWhen(true)] out string? output)
    {
        input = input.Replace("\"", string.Empty);

        if (string.IsNullOrWhiteSpace(input))
        {
            output = null;
            return false;
        }

        _logger.Debug("Requesting translation of text \"{input}\"", input);
        try
        {
            var result = _client.SendText(input).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.Warning("Failed translation of text \"{input}\", no output received", input);
                output = null;
                return false;
            }

            _logger.Debug("Translated text \"{input}\" to \"{output}\"", input, result);
            output = result;
            return true;
        } catch (Exception ex)
        {
            _logger.Error(ex, "Failed translation of text \"{input}\"", input);
            SetFault(ex);
            output = null;
            return false;
        }
    }
    #endregion
}