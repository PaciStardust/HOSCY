using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Audio;

public class EmptyAudioPlaybackDeviceProxy(ILogger logger) : IAudioPlaybackDeviceProxy
{
    private readonly ILogger _logger = logger;
    public MemoryStream Stream { get; private init; } = new();
    public bool IsRunning { get; private set; } = false;

    public void ClearStream()
    {
        Stream.Position = 0;
        Stream.SetLength(0);
        Stream.Capacity = 0;
    }

    public void Dispose()
    {
        _logger.Verbose("Disposing empty audio playback");
        Stream.Dispose();
    }

    public string? GetDeviceName()
    {
        return IsRunning ? AudioUtils.EMPTY_DEVICE : null;
    }

    public async Task<Res> PlayAsync(float volume, CancellationToken ct)
    {
        ClearStream();
        await Task.Delay(10);
        return ResC.Ok();
    }

    public Res Start()
    {
        IsRunning = true;
        return ResC.Ok();
    }

    public Res Stop()
    {
        IsRunning = false;
        return ResC.Ok();
    }
}