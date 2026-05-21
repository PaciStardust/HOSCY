using HoscyCore.Services.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Media.Core;

public abstract class MediaBackendBase(ILogger logger) : StartStopModuleBase(logger), IMediaBackend
{
    public event Action<MediaUpdateInfo> OnMediaUpdate = delegate { };
    private string _lastSent = string.Empty;
    private readonly Lock _lock = new();
    protected void InvokeMediaUpdate(MediaUpdateInfo info)
    {
        if (!(info.Playing is not null || info.Track is not null)) return;

        lock (_lock)
        {
            var infoStr = info.ToString();
            if (infoStr == _lastSent) return;

            _lastSent = infoStr;
            _logger.Debug("Invoking media update ({info})", infoStr);
            OnMediaUpdate.Invoke(info);
        }
    }

    public abstract bool CanGetEndpoints { get; }
    public abstract Task<Res<string[]>> GetEndpointNames();

    public abstract Task<Res> NextAsync();
    public abstract Task<Res> PauseAsync();
    public abstract Task<Res> PlayAsync();
    public abstract Task<Res> PlayPauseAsync();
    public abstract Task<Res> PreviousAsync();
}