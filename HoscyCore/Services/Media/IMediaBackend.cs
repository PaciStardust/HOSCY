using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Media;

public interface IMediaBackendStartInfo : ISoloModuleStartInfo
{
    public MediaBackendConfigFlags ConfigFlags { get; }
}

[Flags]
public enum MediaBackendConfigFlags
{
    None        = 0b0,
    Windows     = 0b1,
    LinuxMpris  = 0b10,
}

public interface IMediaBackend : IStartStopModule
{
    public event Action<MediaUpdateInfo> OnMediaUpdate;

    public bool CanGetEndpoints { get; }
    public Task<Res<string[]>> GetEndpointNames(); 

    public Task<Res> PlayAsync();
    public Task<Res> PauseAsync();
    public Task<Res> NextAsync();
    public Task<Res> PreviousAsync();
    public Task<Res> PlayPauseAsync();
}