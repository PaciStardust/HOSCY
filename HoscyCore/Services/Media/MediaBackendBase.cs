using HoscyCore.Services.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Media;

public abstract class MediaBackendBase(ILogger logger) : StartStopModuleBase(logger), IMediaBackend
{
    public event Action<MediaUpdateInfo> OnMediaUpdate = delegate { };

    public abstract bool CanGetEndpoints { get; }
    public abstract Task<Res<string[]>> GetEndpointNames();

    public abstract Task<Res> NextAsync();
    public abstract Task<Res> PauseAsync();
    public abstract Task<Res> PlayAsync();
    public abstract Task<Res> PlayPauseAsync();
    public abstract Task<Res> PreviousAsync();
}