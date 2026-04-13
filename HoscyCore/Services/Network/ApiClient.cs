using System.Text;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Network;

/// <summary>
/// Wrapper for IWebClient to use with ApiPresets, generic for logging
/// </summary>
[LoadIntoDiContainer(typeof(IApiClient), Lifetime.Transient)]
public class ApiClient(IWebClient webClient, ILogger logger) : IApiClient
{
    private readonly IWebClient _client = webClient;
    private readonly ILogger _logger = logger.ForContext<ApiClient>();
    private string _identifier = "Api";
    private ApiPresetModel? _currentPreset = null;

    #region Loading
    public bool IsPresetLoaded()
    {
        return _currentPreset is not null;
    }

    public bool IsPresetValid()
    {
        return _currentPreset is not null && _currentPreset.IsValid();
    }

    public IApiClient AddIdentifier(string identifier)
    {
        _identifier = identifier;
        return this;
    }

    public Res LoadPreset(ApiPresetModel preset)
    {
        _logger.Debug("{id}: Loading new ApiPreset \"{name}\"", _identifier, preset.Name);
        if (preset.Equals(_currentPreset))
        {
            _logger.Verbose("{id}: Skipped loading new ApiPreset \"{name}\", it is already loaded", _identifier, preset.Name);
            return ResC.Ok();
        }

        ClearPreset();

        if (!preset.IsValid())
        {
            _logger.Error("{id}: Did not load ApiPreset \"{name}\" as it is invalid", _identifier, preset.Name);
            return ResC.Fail(ResMsg.Err($"{_identifier}: Did not load ApiPreset \"{preset.Name}\" as it is invalid"));
        }

        _currentPreset = preset;
        return ResC.Ok();
    }
    #endregion

    #region Sending
    private async Task<Res<string>> SendAsync(HttpContent content, ApiPresetModel preset)
    {
        var health = ClientHealthCheck();
        if (!health.IsOk) return ResC.TFail<string>(health.Msg);
        
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, preset.TargetUrl)
        {
            Content = content
        };

        AddHeaders(requestMessage);

        var jsonIn = await _client.SendAsync(requestMessage, preset.ConnectionTimeout);
        if (!jsonIn.IsOk)
        {
            _logger.Warning("{id}: Received response for request, but json is null ({res})", _identifier, jsonIn);
            return jsonIn;
        }

        var result = OtherUtils.ExtractFromJson(preset.ResultField, jsonIn.Value, logger);
        if (!result.IsOk)
        {
            _logger.Warning("{id}: Unable to find content with key \"{resultField}\" in result \"{jsonIn}\"",
                _identifier, preset.ResultField, jsonIn);
            return ResC.TFail<string>(ResMsg.Err($"Unable to find content with key \"{preset.ResultField}\" in result \"{jsonIn}\""));
        }

        _logger.Verbose("{id}: Found content with value \"{resultField}\": {result}", _identifier, preset.ResultField, result);
        return result;
    }

    public async Task<Res<string>> SendBytesAsync(byte[] bytes)
    {
        _logger.Verbose("{id}: Sending byte request via ApiPreset \"{presetName}\"", _identifier, _currentPreset?.Name ?? "NULL");
        var health = PresetHealthCheck();
        if (!health.IsOk) return ResC.TFail<string>(health.Msg);

        var content = new ByteArrayContent(bytes);
        if (string.IsNullOrWhiteSpace(_currentPreset!.ContentType) || !content.Headers.TryAddWithoutValidation("Content-Type", _currentPreset.ContentType))
        {
            _logger.Warning("{id}: Unable to send data to API as ContentType \"{contentType}\" is invalid, are you using the Type suggested by the API's documentation?", _identifier, _currentPreset.ContentType);
            return ResC.TFail<string>(ResMsg.Err($"Unable to send data to API as ContentType \"{_currentPreset.ContentType}\" is invalid"));
        }

        return await SendAsync(content, _currentPreset);
    }

    public async Task<Res<string>> SendTextAsync(string text)
    {
        _logger.Verbose("{id}: Sending text request via ApiPreset \"{presetName}\"", _identifier, _currentPreset?.Name ?? "NULL");
        var health = PresetHealthCheck();
        if (!health.IsOk) return ResC.TFail<string>(health.Msg);

        var jsonOut = ReplaceToken(_currentPreset!.SentData, "[T]", text);
        if (_currentPreset.SentData == jsonOut)
        {
            _logger.Warning("{id}: Unable to send data to API as JSON contains no token to replace, have you made sure the JSON option contains \"[T]\"?", _identifier);
            var message = $"Unable to send data to API as JSON contains no token to replace, have you made sure the JSON option contains \"[T]\"?";
            return ResC.TFail<string>(ResMsg.Err(message));
        }

        return await SendAsync(new StringContent(jsonOut, Encoding.UTF8, _currentPreset.ContentType), _currentPreset);
    }
    #endregion

    #region Health Check
    private Res ClientHealthCheck()
    {
        if (_client.GetCurrentStatus() == ServiceStatus.Stopped)
        {
            _logger.Warning(messageTemplate: "{id}: Not sending request as WebClient is stopped", _identifier);
            return ResC.Fail(ResMsg.Err("Not sending request as WebClient is stopped"));
        }
        return ResC.Ok();
    }

    private Res PresetHealthCheck()
    {
        if (_currentPreset is null || !_currentPreset.IsValid())
        {
            _logger.Warning(messageTemplate: "{id}: Not sending request as no valid ApiPreset is loaded", _identifier);
            return ResC.Fail(ResMsg.Err("Not sending request as no valid ApiPreset is loaded"));
        }
        return ResC.Ok();
    }
    #endregion

    #region Utils
    private void AddHeaders(HttpRequestMessage content)
    {
        if (_currentPreset == null) return;

        content.Headers.Authorization = _currentPreset.AuthenticationHeader();

        foreach (var headerInfo in _currentPreset.HeaderValues)
        {
            if (!content.Headers.TryAddWithoutValidation(headerInfo.Key, headerInfo.Value))
            {
                _logger.Warning("{id}: Skipped adding header info \"{headerInfoKey}\" : \"{headerInfoValue}\". As it was deemed invalid, it will be removed",
                    _identifier, headerInfo.Key, headerInfo.Value);
                _currentPreset.HeaderValues.Remove(headerInfo.Key);
            }
        }
    }

    public void ClearPreset()
    {
        _logger.Debug("{id}: Clearing current preset", _identifier);
        _currentPreset = null;
    }

    private static string ReplaceToken(string text, string token, string value)
    {
        return text.Replace(token, value);
    }
    #endregion
}