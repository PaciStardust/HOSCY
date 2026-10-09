using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.Utility;

public partial class AudioDeviceComboBoxData : ObservableObject
{
    private const string INDICATOR_MISSING = "[Missing] ";


    [ObservableProperty]
    public partial int PrimaryIndex { get; set; }
    [ObservableProperty]
    public partial string[] PrimaryOptions { get; private set; } = [];

    [ObservableProperty]
    public partial int FallbackIndex { get; set; }
    [ObservableProperty]
    public partial string[] FallbackOptions { get; private set; } = [];

    private readonly ILogger? _logger;
    private readonly string _id;
    private readonly IAudioService _audio;
    private readonly bool _isCapture;
    private readonly IBackToFrontNotifyService _notify;

    public AudioDeviceComboBoxData
    (
        ILogger? logger, 
        IAudioService audio, 
        string id,
        bool isCapture,
        IBackToFrontNotifyService notify,
        string primaryDeviceName,
        string fallbackDeviceName
    )
    {
        _logger = logger;
        _id = id;
        _audio = audio;
        _isCapture = isCapture;
        _notify = notify;

        RefreshItems(primaryDeviceName, fallbackDeviceName);
    }

    public void RefreshItems(string primaryDeviceName, string fallbackDeviceName)
    {
        var devices = _isCapture ? _audio.GetCaptureInfos() : _audio.GetPlaybackInfos();
        if (!devices.IsOk)
        {
            (PrimaryIndex, PrimaryOptions) = SetFailed();
            (FallbackIndex, FallbackOptions) = SetFailed();
            _notify.SendResult("Failed to Load Devices", devices.Msg);
            return;
        }

        var deviceInfos = devices.Value;
        var deviceNames = deviceInfos.Select(x => x.Name)
            .Append(AudioUtils.DEVICE_DEFAULT)
            .Append(AudioUtils.DEVICE_NONE)
            .ToArray();

        (PrimaryIndex, PrimaryOptions) = SetComboBox(primaryDeviceName);
        (FallbackIndex, FallbackOptions) = SetComboBox(fallbackDeviceName);

        (int Index, string[] Options) SetComboBox(string deviceName)
        {
            if (AudioUtils.IsDefaultDevice(deviceName))
                return (deviceNames.Length - 2, deviceNames);

            if (AudioUtils.IsNoneDevice(deviceName))
                return (deviceNames.Length - 1, deviceNames);

            var match = AudioUtils.GetDeviceInfoForName
                (_logger, _id, deviceInfos, deviceName);

            if (match is not null)
            {
                var idx = deviceInfos.GetListIndex(x => x == match);
                if (idx > -1)
                {
                    return (idx, deviceNames);
                }
            }

            var list = deviceNames.ToList();
            list.Insert(0, $"{INDICATOR_MISSING} {deviceName}");
            return (0, list.ToArray());
        }

        (int Index, string[] Options) SetFailed()
        {
            return (0, ["(Devices failed to load)"]);
        }
    }

    public void OnSelectionChanged(ref string deviceName, bool isFallback)
    {
        var (idx, options) = isFallback 
            ? (FallbackIndex, FallbackOptions)
            : (PrimaryIndex, PrimaryOptions);

        if (idx == -1) return;
        if (idx >= options.Length)
        {
            _logger?.Warning($"Selected index for combo box {_id} too big");
            return;
        }

        var selected = options[idx];
        if (!selected.Equals(deviceName, System.StringComparison.OrdinalIgnoreCase))
        {
            _logger?.Information("Set {dev} for id {id} to \"{name}\"", isFallback ? "fallback" : "primary", _id, deviceName);
            deviceName = selected;
        }
    }
}