using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Media.Core;

public interface IMediaControlService : ISoloModuleManager<IMediaBackendStartInfo>
{
    public event Action<MediaUpdateInfo> OnMediaUpdate;

    public bool CanGetEndpoints { get; }
    public Task<Res<string[]>> GetEndpointNamesAsync(); 

    public Task<Res> PlayAsync();
    public Task<Res> PauseAsync();
    public Task<Res> NextAsync();
    public Task<Res> PreviousAsync();
    public Task<Res> PlayPauseAsync();
}