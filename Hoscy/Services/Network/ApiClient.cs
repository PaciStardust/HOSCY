using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Hoscy.Configuration.Modern;
using Hoscy.Utility;
using Serilog;

namespace Hoscy.Services.Network;

/// <summary>
/// Wrapper for IWebClient to use with ApiPresets, generic for logging
/// </summary>
public class ApiClient<T>(IWebClient webClient, ILogger logger) //todo: notify?
{
    private readonly IWebClient _client = webClient;
    private readonly ILogger _logger = logger.ForContext<ApiClient<T>>();

    private ApiPresetModel? _currentPreset = null;

    #region Loading
    public bool LoadPreset(ApiPresetModel preset)
    {
        _logger.Information("Loading new ApiPreset {name}", preset.Name);
        if (preset.Equals(_currentPreset))
        {
            _logger.Information("Skipped loading new ApiPreset {name}, it is already loaded", preset.Name);
            return true;
        }

        ClearPreset();

        if (!preset.IsValid())
        {
            _logger.Error("Did not load ApiPreset {name} as it is invalid", preset.Name);
            return false;
        }

        _currentPreset = preset;
        return true;
    }
    #endregion

    #region Sending
    private async Task<string?> Send(HttpContent content)
    {
        if (_currentPreset is null || !_currentPreset.IsValid())
        {
            _logger.Warning("Not sending request as no valid ApiPreset is loaded");
            return null;
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, _currentPreset.TargetUrl)
        {
            Content = content
        };

        AddHeaders(requestMessage);

        var jsonIn = await _client.SendAsync(requestMessage, _currentPreset.ConnectionTimeout);

        if (jsonIn is null)
        {
            _logger.Warning("Received response for reques, but json is null, returning null");
            return null;
        }

        var result = Utils.ExtractFromJson(_currentPreset.ResultField, jsonIn);
        if (result is null)
        {
            _logger.Warning("Unable to find content with value {resultField}, returning null", _currentPreset.ResultField);
        }
        else
        {
            _logger.Debug("Found content with value {resultField}: {result}", _currentPreset.ResultField, result);
        }
        return result;
    }
    
    internal async Task<string?> SendBytes(byte[] bytes)
    {
        _logger.Debug("Sending byte request via ApiPreset {presetName}", _currentPreset?.Name ?? "NULL");
        if (_currentPreset is null || !_currentPreset.IsValid())
        {
            _logger.Warning("Not sending byte request as no valid ApiPreset is loaded");
            return null;
        }

        var content = new ByteArrayContent(bytes);
        if (string.IsNullOrWhiteSpace(_currentPreset.ContentType) || !content.Headers.TryAddWithoutValidation("Content-Type", _currentPreset.ContentType))
        {
            _logger.Warning("Unable to send data to API as ContentType {contentType} is invalid, are you using the Type suggested by the API's documentation?", _currentPreset.ContentType);
            return null;
        }

        return await Send(content);
    }

    internal async Task<string?> SendText(string text)
    {
        _logger.Debug("Sending text request via ApiPreset {presetName}", _currentPreset?.Name ?? "NULL");
        if (_currentPreset is null || !_currentPreset.IsValid())
        {
            _logger.Warning(messageTemplate: "Not sending text request as no valid ApiPreset is loaded");
            return null;
        }

        var jsonOut = ReplaceToken(_currentPreset.SentData, "[T]", text);
        if (_currentPreset.SentData == jsonOut)
        {
            _logger.Warning("Unable to send data to data to API as JSON contains no token, have you made sure the JSON option contains \"[T]\"?");
            return null;
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
                _logger.Warning($"Skipped adding header info \"{headerInfo.Key} : {headerInfo.Value}\". As it was deemed invalid, it will be removed");
                _currentPreset.HeaderValues.Remove(headerInfo);
            }
        }
    }

    public void ClearPreset()
    {
        _logger.Debug("Cleaing current preset");
        _currentPreset = null;
    }

    private static string ReplaceToken(string text, string token, string value)
    {
        return text.Replace(token, value);
    }
    #endregion
}