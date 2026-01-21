using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Utility;

namespace HoscyCore.Configuration.Modern;

public class OscRelayFilterModel : ObservableObject
{
    private const string NO_FILTER_NAME = "Unnamed Filter";
    private string _name = NO_FILTER_NAME;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? NO_FILTER_NAME : value);
    }

    private ushort _port = ushort.MinValue;
    public ushort Port
    {
        get => _port;
        set => SetProperty(ref _port, value.MinMax(ushort.MinValue, ushort.MaxValue));
    }

    private string _ip = "127.0.0.1";
    public string Ip
    {
        get => _ip;
        set => SetProperty(ref _ip, value);
    }

    private ObservableCollection<string> _filters = [];
    public ObservableCollection<string> Filters
    {
        get => _filters;
        set => SetProperty(ref _filters, value);
    }

    private bool _blacklistMode;
    public bool BlacklistMode //todo: [TEST]
    {
        get => _blacklistMode;
        set => SetProperty(ref _blacklistMode, value);
    }

    private bool _isValid = true;
    public override string ToString()
    {
        return $"{(_isValid ? "" : "[x]")}{Name} ={(BlacklistMode ? "B" : string.Empty)}> {Ip}:{Port}";
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