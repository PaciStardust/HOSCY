using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Utility;

namespace HoscyCore.Configuration.Modern;

public class CounterModel : ObservableObject
{
    public const string DESC_Name = "Text to be displayed in notification and logs";
    private const string NO_COUNTER_NAME = "Unnamed Counter";
    private string _name = NO_COUNTER_NAME;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public const string DESC_Count = "Amount triggered";
    public const uint MIN_Count = uint.MinValue;
    public const uint MAX_Count = uint.MaxValue;
    private uint _count;
    public uint Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    public const string DESC_LastUsed = "Time the counter was last triggered";
    private DateTimeOffset _lastUsed = DateTimeOffset.MinValue;
    public DateTimeOffset LastUsed
    {
        get => _lastUsed;
        set => SetProperty(ref _lastUsed, value);
    }

    public const string DESC_Enabled = "Toggles if counter is in use";
    private bool _enabled = true;
    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public const string DESC_DoDisplay = "Toggles if counter increases are sent as notification";
    private bool _doDisplay = true;
    public bool DoDisplay
    {
        get => _doDisplay;
        set => SetProperty(ref _doDisplay, value);
    }

    public const string DESC_CooldownSeconds = "Seconds the counter can not increase after trigger";
    public const float MIN_CooldownSeconds = 0;
    public const float MAX_CooldownSeconds = 3_600;
    private float _cooldown;
    public float CooldownSeconds
    {
        get => _cooldown;
        set => SetProperty(ref _cooldown, 
            value.MinMax(MIN_CooldownSeconds, MAX_CooldownSeconds));
    }

    public const string DESC_Parameter = "OSC path to trigger on";
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
    public string FullParameter() => _fullParameter;

    public void Increase()
    {
        Count++;
        LastUsed = DateTimeOffset.UtcNow;
    }

    public override string ToString()
    {
        return $"{(Enabled ? DoDisplay ? "" : "[h]" : "[x] ")}{Name}: {Count:N0}";
    }
}