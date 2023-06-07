using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Hoscy.Models
{
    internal class ApiPresetModel
    {
        /// <summary>
        /// THIS CLASS IS USED IN CONFIG - IT CAN NOT LOG
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? "Unnamed Preset" : value;
        }
        private string _name = "Unnamed Preset";

        public string SentData { get; set; } = @"{""data"" : ""[T]""}";
        public Dictionary<string, string> HeaderValues { get; set; } = new();
        public string ContentType { get; set; } = "application/json";
        public string ResultField { get; set; } = "result";

        public string TargetUrl
        {
            get => _targetUrl;
            set
            {
                _targetUrl = value;
                _fullTargetUrl = value.StartsWith("http") ? value : "https://" + value;
            }
        }
        private string _targetUrl = string.Empty;
        private string _fullTargetUrl = string.Empty;

        public string Authorization
        {
            get => _authorization;
            set
            {
                _authorization = string.Empty;
                _authenticationHeader = null;

                if (string.IsNullOrWhiteSpace(value))
                    return;

                try
                {
                    _authorization = value;
                    var authSplit = value.Split(' ');

                    if (authSplit.Length == 1)
                        _authenticationHeader = new(authSplit[0]);
                    else if (authSplit.Length > 1)
                        _authenticationHeader = new(authSplit[0], string.Join(' ', authSplit[1..]));
                }
                catch { }
            }
        }
        private string _authorization = string.Empty;
        private AuthenticationHeaderValue? _authenticationHeader = null;

        public int ConnectionTimeout
        {
            get => _connectionTimeout;
            set => _connectionTimeout = Utils.MinMax(value, 25, 60000);
        }
        private int _connectionTimeout = 3000;

        internal string FullTargetUrl() => _fullTargetUrl;
        internal AuthenticationHeaderValue? AuthenticationHeader() => _authenticationHeader;

        internal bool IsValid()
            => !string.IsNullOrWhiteSpace(TargetUrl)
            && !string.IsNullOrWhiteSpace(SentData)
            && !string.IsNullOrWhiteSpace(ResultField)
            && !string.IsNullOrWhiteSpace(ContentType);
    }
}
