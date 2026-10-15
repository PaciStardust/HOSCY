namespace HoscyCore.Services.Audio;

public enum AudioPlaybackDeviceState
{
    SwapPending,
    Swapping,
    Error,
    Stopped,
    Running
}

public readonly record struct AudioPlaybackDeviceStatusInfo(AudioPlaybackDeviceState State, string? Name)
{
    public override string ToString()
    {
        var stateText = State switch
        {
            AudioPlaybackDeviceState.SwapPending => "Swap Pending",
            AudioPlaybackDeviceState.Swapping => "Currently Swapping",
            AudioPlaybackDeviceState.Error => "Faulted",
            AudioPlaybackDeviceState.Stopped => "Stopped",
            AudioPlaybackDeviceState.Running => string.Empty,
            _ => "Unknown"
        };
        return $"{Name ?? "Nameless Device"}{(stateText.Length == 0 ? string.Empty : $" ({stateText})")}";
    }
}