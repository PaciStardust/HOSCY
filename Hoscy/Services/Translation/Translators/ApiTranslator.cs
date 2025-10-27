using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Network;
using Hoscy.Services.Translation.Core;
using Serilog;

namespace Hoscy.Services.Translation.Translators;

public class ApiTranslator(ILogger logger, ConfigModel config, IApiClient client) : ITranslator //todo: base, use
{
    private readonly ILogger _logger = logger.ForContext<ApiTranslator>();
    private readonly ConfigModel _config = config;
    private readonly IApiClient _client = client.AddIdentifier(nameof(ApiTranslator));

    public event EventHandler<Exception> OnRuntimeError = delegate {};
    public event EventHandler OnShutdownCompleted = delegate { };

    private Exception? _runtimeException = null; //todo: needed?

    #region Info
    public string GetIdentifier()
        => "Api Translator";
    #endregion

    #region Start/Stop
    public void Start()
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

    public void Stop()
    {
        _logger.Information("Stopping Translator");
        if (_client.IsPresetLoaded())
        {
            _client.ClearPreset();
        }
        _logger.Information("Stopped Translator");
    }

    public void Restart()
    {
        _logger.Information("Restarting Translator");
        Stop();
        Start();
        _logger.Information("Restarted Translator");
    }

    public bool IsRunning()
    {
        return _client.IsPresetLoaded();
    }

    public Exception? GetFaultIfExists()
        => _runtimeException;

    public StartStopStatus GetStatus()
    {
        if (!IsRunning()) return StartStopStatus.Stopped;
        if (GetFaultIfExists() is not null) return StartStopStatus.Faulted;
        return StartStopStatus.Running;
    }
    #endregion

    #region Functionality
    public bool TryTranslate(string input, [NotNullWhen(true)] out string? output)
    {
        input = input.Replace("\"", string.Empty);

        if (string.IsNullOrWhiteSpace(input))
        {
            output = null;
            return false;
        }

        //todo: implement in base?
        // if (input.Length > _config.ApiCommunication_Translation_MaxTextLength)
        // {
        //     if (_config.ApiCommunication_Translation_SkipLongerMessages)
        //     {
        //         output = null;
        //         return false;
        //     }
        // }

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
            _runtimeException = ex;
            OnRuntimeError.Invoke(this, ex);
            output = null;
            return false;
        }
    }
    #endregion
}