using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Utility;

namespace HoscyCore.Configuration.Modern;

public class CounterModel : ObservableObject
{
    private const string NO_COUNTER_NAME = "Unnamed Counter";
    private string _name = NO_COUNTER_NAME;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private uint _count;
    public uint Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    private DateTimeOffset _lastUsed = DateTimeOffset.MinValue;
    public DateTimeOffset LastUsed
    {
        get => _lastUsed;
        set => SetProperty(ref _lastUsed, value);
    }

    private bool _enabled = true;
    public bool Enabled //todo: [TEST]
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    private bool _doDisplay = true;
    public bool DoDisplay //todo: [TEST]
    {
        get => _doDisplay;
        set => SetProperty(ref _doDisplay, value);
    }

    private float _cooldown;
    public float CooldownSeconds
    {
        get => _cooldown;
        set => SetProperty(ref _cooldown, value.MinMax(0, 3600));
    }

    private string _parameter = "Parameter";
    private string _fullParameter = "/avatar/parameters/Parameter";
    public string Parameter
    {
        get => _parameter;
        set
        {
            SetProperty(ref _parameter, value);
            SetProperty(ref _fullParameter, value.StartsWith('/') ? value : "/avatar/parameters/" + value);
        }
    }
    internal string FullParameter() => _fullParameter;

    internal void Increase()
    {
        Count++;
        LastUsed = DateTimeOffset.UtcNow;
    }

    public override string ToString()
    {
        return $"{(Enabled ? "" : "[x] ")}{Name}: {Count:N0}";
    }
}