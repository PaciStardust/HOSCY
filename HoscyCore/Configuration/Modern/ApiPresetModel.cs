using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Utility;
using System.Net.Http.Headers;

namespace HoscyCore.Configuration.Modern;

public class ApiPresetModel : ObservableObject
{
    public const string DESC_Name = "Name of the Preset to be displayed in logs and drop-downs";
    private const string NO_PRESET = "Unnamed Preset";
    private string _name = NO_PRESET;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? NO_PRESET : value);
    }

    public const string DESC_SentData = "JSON/Text data to be sent to URL, \"[T]\" will be resplaced with the text to send";
    private string _sentData = @"{""data"" : ""[T]""}";
    public string SentData
    {
        get => _sentData;
        set => SetProperty(ref _sentData, value);
    }

    public const string DESC_HeaderValues = "Headers the data will be sent with, like Auth";
    public Dictionary<string, string> _headerValues = [];
    public Dictionary<string, string> HeaderValues
    {
        get => _headerValues;
        set => SetProperty(ref _headerValues, value);
    }

    public const string DESC_ContentType = "Content type of the request to be sent";
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

    public const string DESC_TargetUrl = "URL the request will be sent to";
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

    public const string DESC_Authorization = "Auth header to be sent with the request";
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
            catch
            {
                SetProperty(ref _authorization, string.Empty);
                SetProperty(ref _authenticationHeader, null);
            }
        }
    }
    internal AuthenticationHeaderValue? AuthenticationHeader() => _authenticationHeader;

    public const string DESC_ConnectionTimeout = "MS to wait for an answer to the request before giving up";
    public const int MIN_ConnectionTimeout = 25;
    public const int MAX_ConnectionTimeout = 60_000;
    private int _connectionTimeout = 3000;
    public int ConnectionTimeout
    {
        get => _connectionTimeout;
        set => SetProperty(ref _connectionTimeout, 
            value.MinMax(MIN_ConnectionTimeout, MAX_ConnectionTimeout));
    }

    internal bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(TargetUrl)
        && !string.IsNullOrWhiteSpace(SentData)
        && !string.IsNullOrWhiteSpace(ResultField)
        && !string.IsNullOrWhiteSpace(ContentType);
    }

    public override string ToString()
    {
        return $"{Name} ({TargetUrl})";
    }
}