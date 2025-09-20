using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;

namespace Hoscy.Models.Config;

public class ApiPresetModel : ObservableObject
{
    private const string NO_PRESET = "Unnamed Preset";
    private string _name = NO_PRESET;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? NO_PRESET : value);
    }

    private string _sentData = @"{""data"" : ""[T]""}";
    public string SentData
    {
        get => _sentData;
        set => SetProperty(ref _sentData, value);
    }

    public ObservableCollection<KeyValuePair<string, string>> _headerValues = [];
    public ObservableCollection<KeyValuePair<string, string>> HeaderValues
    {
        get => _headerValues;
        set => SetProperty(ref _headerValues, value);
    }

    private string _contentType = "application/json";
    public string ContentType
    {
        get => _contentType;
        set => SetProperty(ref _contentType, value);
    }

    private string _resultField = "result";
    public string ResultField
    {
        get => _resultField;
        set => SetProperty(ref _resultField, value);
    }

    private string _targetUrl = string.Empty;
    private string _fullTargetUrl = string.Empty;
    public string TargetUrl
    {
        get => _targetUrl;
        set
        {
            SetProperty(ref _targetUrl, value);
            SetProperty(ref _fullTargetUrl, value.StartsWith("http") ? value : "https://" + value);
        }
    }
    internal string FullTargetUrl() => _fullTargetUrl;

    private string _authorization = string.Empty;
    private AuthenticationHeaderValue? _authenticationHeader = null;
    public string Authorization
    {
        get => _authorization;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            try
            {
                SetProperty(ref _authorization, value);
                var authSplit = value.Split(' ');
                SetProperty(ref _authenticationHeader, authSplit.Length == 1
                    ? new(authSplit[0])
                    : new(authSplit[0], string.Join(' ', authSplit[1..]))
                );
            }
            catch { }
            finally
            {
                SetProperty(ref _authorization, string.Empty);
                SetProperty(ref _authenticationHeader, null);
            }
        }
    }
    internal AuthenticationHeaderValue? AuthenticationHeader() => _authenticationHeader;

    private int _connectionTimeout = 3000;
    public int ConnectionTimeout
    {
        get => _connectionTimeout;
        set => SetProperty(ref _connectionTimeout, Utils.MinMax(value, 25, 60000));
    }

    internal bool IsValid()
        => !string.IsNullOrWhiteSpace(TargetUrl)
        && !string.IsNullOrWhiteSpace(SentData)
        && !string.IsNullOrWhiteSpace(ResultField)
        && !string.IsNullOrWhiteSpace(ContentType);
}