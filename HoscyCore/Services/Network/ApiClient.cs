using System.Text;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Network;

/// <summary>
/// Wrapper for IWebClient to use with ApiPresets, generic for logging
/// </summary>
[LoadIntoDiContainer(typeof(IApiClient), Lifetime.Transient)]
public class ApiClient(IWebClient webClient, ILogger logger) : IApiClient //todo: [TEST] Does loading work?
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

    public IApiClient AddIdentifier(string identifier)
    {
        _identifier = identifier;
        return this;
    }

    public bool LoadPreset(ApiPresetModel preset)
    {
        _logger.Information("{id}: Loading new ApiPreset \"{name}\"", _identifier, preset.Name);
        if (preset.Equals(_currentPreset))
        {
            _logger.Information("{id}: Skipped loading new ApiPreset \"{name}\", it is already loaded", _identifier, preset.Name);
            return true;
        }

        ClearPreset();

        if (!preset.IsValid())
        {
            _logger.Error("{id}: Did not load ApiPreset \"{name}\" as it is invalid", _identifier, preset.Name);
            return false;
        }

        _currentPreset = preset;
        return true;
    }
    #endregion

    #region Sending
    private async Task<string> Send(HttpContent content)
    {
        if (_currentPreset is null || !_currentPreset.IsValid())
        {
            _logger.Warning("{id}: Not sending request as no valid ApiPreset is loaded", _identifier);
            throw new InvalidOperationException("No valid ApiPreset loaded");
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, _currentPreset.TargetUrl)
        {
            Content = content
        };

        AddHeaders(requestMessage);

        var jsonIn = await _client.SendAsync(requestMessage, _currentPreset.ConnectionTimeout);

        if (jsonIn is null)
        {
            _logger.Warning("{id}: Received response for request, but json is null", _identifier);
            throw new HttpRequestException("Received response for request, but json is null");
        }

        var result = OtherUtils.ExtractFromJson(_currentPreset.ResultField, jsonIn);
        if (result is null)
        {
            _logger.Warning("{id}: Unable to find content with key \"{resultField}\" in result \"{jsonIn}\"", _identifier, _currentPreset.ResultField, jsonIn);
            throw new HttpRequestException($"Unable to find content with key \"{_currentPreset.ResultField}\" in result \"{jsonIn}\"");
        }
        else
        {
            _logger.Debug("{id}: Found content with value \"{resultField}\": {result}", _identifier, _currentPreset.ResultField, result);
        }
        return result;
    }

    public async Task<string> SendBytes(byte[] bytes)
    {
        _logger.Debug("{id}: Sending byte request via ApiPreset \"{presetName}\"", _identifier, _currentPreset?.Name ?? "NULL");
        if (_currentPreset is null || !_currentPreset.IsValid())
        {
            _logger.Warning("{id}: Not sending byte request as no valid ApiPreset is loaded", _identifier);
            throw new InvalidOperationException("No valid ApiPreset loaded");
        }

        var content = new ByteArrayContent(bytes);
        if (string.IsNullOrWhiteSpace(_currentPreset.ContentType) || !content.Headers.TryAddWithoutValidation("Content-Type", _currentPreset.ContentType))
        {
            _logger.Warning("{id}: Unable to send data to API as ContentType \"{contentType}\" is invalid, are you using the Type suggested by the API's documentation?", _identifier, _currentPreset.ContentType);
            throw new ArgumentException($"Unable to send data to API as ContentType \"{_currentPreset.ContentType}\" is invalid");
        }

        return await Send(content);
    }

    public async Task<string> SendText(string text)
    {
        _logger.Debug("{id}: Sending text request via ApiPreset \"{presetName}\"", _identifier, _currentPreset?.Name ?? "NULL");
        if (_currentPreset is null || !_currentPreset.IsValid())
        {
            _logger.Warning(messageTemplate: "{id}: Not sending text request as no valid ApiPreset is loaded", _identifier);
            throw new InvalidOperationException("No valid ApiPreset loaded");
        }

        var jsonOut = ReplaceToken(_currentPreset.SentData, "[T]", text);
        if (_currentPreset.SentData == jsonOut)
        {
            _logger.Warning("{id}: Unable to send data to API as JSON contains no token to replace, have you made sure the JSON option contains \"[T]\"?", _identifier);
            throw new ArgumentException($"Unable to send data to API as JSON contains no token to replace, have you made sure the JSON option contains \"[T]\"?");
        }

        return await Send(new StringContent(jsonOut, Encoding.UTF8, _currentPreset.ContentType));
    }
    #endregion

    #region Utils
    private void AddHeaders(HttpRequestMessage content)
    {
        if (_currentPreset == null)
            return;

        content.Headers.Authorization = _currentPreset.AuthenticationHeader();

        foreach (var headerInfo in _currentPreset.HeaderValues)
        {
            if (!content.Headers.TryAddWithoutValidation(headerInfo.Key, headerInfo.Value))
            {
                _logger.Warning("{id}: Skipped adding header info \"{headerInfoKey}\" : \"{headerInfoValue}\". As it was deemed invalid, it will be removed", _identifier, headerInfo.Key, headerInfo.Value);
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