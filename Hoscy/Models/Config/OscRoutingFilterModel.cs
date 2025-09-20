using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoscy.Models.Config;

public class OscRoutingFilterModel : ObservableObject
{
    private const string NO_FILTER_NAME = "Unnamed Filter";
    private string _name = NO_FILTER_NAME;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? NO_FILTER_NAME : value);
    }

    private int _port = -1;
    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, Utils.MinMax(value, -1, 65535));
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
    public bool BlacklistMode
    {
        get => _blacklistMode;
        set => SetProperty(ref _blacklistMode, value);
    }

    private bool _isValid = true;
    public override string ToString()
        => $"{(_isValid ? "" : "[x]")}{Name} ={(BlacklistMode ? "B" : string.Empty)}> {Ip}:{Port}";

    /// <summary>
    /// Sets validity to be displayed in filter window
    /// </summary>
    internal void SetValidity(bool state)
        => _isValid = state;
}