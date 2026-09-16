using HoscyCore.Services.Media;
using HoscyCore.Services.Media.Core;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockMediaControlService : MockSoloModuleManagerBase<IMediaBackendStartInfo>, IMediaControlService
{
    public bool CanGetEndpoints { get; set; } = false;

    public event Action<MediaUpdateInfo> OnMediaUpdate = delegate { };

    public List<string> Endpoints { get; set; } = [];
    public Res<string[]> GetEndpointNames()
    {
        ThrowIfNeeded();
        return ResC.TOk(Endpoints.ToArray());
    }

    public int CalledNext { get; private set; }
    public Task<Res> NextAsync()
    {
        CalledNext++;
        ThrowIfNeeded();
        return Task.FromResult(ResC.Ok());
    }

    public int CalledPause { get; private set; }
    public Task<Res> PauseAsync()
    {
        CalledPause++;
        ThrowIfNeeded();
        return Task.FromResult(ResC.Ok());
    }

    public int CalledPlay { get; private set; }
    public Task<Res> PlayAsync()
    {
        CalledPlay++;
        ThrowIfNeeded();
        return Task.FromResult(ResC.Ok());
    }

    public int CalledPlayPause { get; private set; }
    public Task<Res> PlayPauseAsync()
    {
        CalledPlayPause++;
        ThrowIfNeeded();
        return Task.FromResult(ResC.Ok());
    }

    public int CalledPrevious { get; private set; }
    public Task<Res> PreviousAsync()
    {
        CalledPrevious++;
        ThrowIfNeeded();
        return Task.FromResult(ResC.Ok());
    }

    public bool ThrowOnce { get; set; }
    private void ThrowIfNeeded()
    {
        if (ThrowOnce)
        {
            ThrowOnce = false;
            throw new Exception("Test Exception");
        } 
    }

    public void Reset()
    {
        ThrowOnce = false;
        Endpoints = [];

        CalledNext = 0;
        CalledPause = 0;
        CalledPlay = 0;
        CalledPlayPause = 0;
        CalledPrevious = 0;
    }
}