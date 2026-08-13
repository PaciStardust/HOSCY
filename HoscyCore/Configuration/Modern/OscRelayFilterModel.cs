using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Utility;

namespace HoscyCore.Configuration.Modern;

public class OscRelayFilterModel : ObservableObject
{
    public const string DESC_Name = "Text to be displayed in logs";
    private const string NO_FILTER_NAME = "Unnamed Filter";
    private string _name = NO_FILTER_NAME;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? NO_FILTER_NAME : value);
    }

    public const string DESC_Port = "Target port for relaying OSC";
    public const ushort MIN_Port = ushort.MinValue;
    public const ushort MAX_Port = ushort.MinValue;
    private ushort _port = ushort.MinValue;
    public ushort Port
    {
        get => _port;
        set => SetProperty(ref _port,
            value.MinMax(MIN_Port, MAX_Port));
    }

    public const string DESC_Ip = "Target IP for relaying OSC";
    private string _ip = "127.0.0.1";
    public string Ip
    {
        get => _ip;
        set => SetProperty(ref _ip, value);
    }

    public const string DESC_Filters = "Filters for what should be relayed or not be relayed (if blacklist)";
    private ObservableCollection<string> _filters = [];
    public ObservableCollection<string> Filters
    {
        get => _filters;
        set => SetProperty(ref _filters, value);
    }

    public const string DESC_BlacklistMode = "Sets weather or not the filters allow or block (if blacklist)";
    private bool _blacklistMode;
    public bool BlacklistMode
    {
        get => _blacklistMode;
        set => SetProperty(ref _blacklistMode, value);
    }

    public const string DESC_Enabled = "Sets if the relay is in use";
    private bool _enabled = true;
    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    private bool _isValid = true;
    public override string ToString()
    {
        return $"{(_enabled ? (_isValid ? "" : "[x]") : "[_]")}{Name} ={(BlacklistMode ? "B" : string.Empty)}> {Ip}:{Port}";
    }

    /// <summary>
    /// Sets validity to be displayed in filter window
    /// </summary>
    public void SetValidity(bool state)
    {
        _isValid = state;
    }

    /// <summary>
    /// Gets validity to be displayed in filter window
    /// </summary>
    public bool GetValidity()
    {
        return _isValid;
    }
}