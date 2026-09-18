using HoscyCore.Services.Media;
using HoscyCore.Services.Media.Core;
using HoscyCore.Services.Translation.Core;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockMediaBackendStartInfo : MockSoloModuleStartInfoBase, IMediaBackendStartInfo
{
    public MediaBackendConfigFlags ConfigFlags => MediaBackendConfigFlags.None;
}


public abstract class MockMediaBackend : MockStartStopModuleBase, IMediaBackend
{
    public bool DoThrowNext { get; set; } = false;
    private ResMsg? GetError()
    {
        if (DoThrowNext)
        {
            DoThrowNext = false;
            return ResMsg.Err("Test");
        }
        return null;
    }

    public bool CanGetEndpoints { get; set; }

    public event Action<MediaUpdateInfo> OnMediaUpdate = delegate { };
    public void InvokeMediaUpdate(MediaUpdateInfo media)
    {
        OnMediaUpdate.Invoke(media);
    }

    public string[] Endpoints { get; set; } = [];
    public Res<string[]> GetEndpointNames()
    {
        var err = GetError();
        return err is null ? ResC.TOk(Endpoints) : ResC.TFail<string[]>(err);
    }


    public MockMediaBackendAction LastReceivedAction { get; set; }
    public Task<Res> NextAsync()
    {
        LastReceivedAction = MockMediaBackendAction.Next;
        var err = GetError();
        return Task.FromResult(err is null ? ResC.Ok() : ResC.Fail(err));
    }

    public Task<Res> PauseAsync()
    {
        LastReceivedAction = MockMediaBackendAction.Pause;
        var err = GetError();
        return Task.FromResult(err is null ? ResC.Ok() : ResC.Fail(err));
    }

    public Task<Res> PlayAsync()
    {
        LastReceivedAction = MockMediaBackendAction.Play;
        var err = GetError();
        return Task.FromResult(err is null ? ResC.Ok() : ResC.Fail(err));
    }

    public Task<Res> PlayPauseAsync()
    {
        LastReceivedAction = MockMediaBackendAction.PlayPause;
        var err = GetError();
        return Task.FromResult(err is null ? ResC.Ok() : ResC.Fail(err));
    }

    public Task<Res> PreviousAsync()
    {
        LastReceivedAction = MockMediaBackendAction.Previous;
        var err = GetError();
        return Task.FromResult(err is null ? ResC.Ok() : ResC.Fail(err));
    }

    public override void ResetStats()
    {
        LastReceivedAction = MockMediaBackendAction.Nothing;
        DoThrowNext = false;
        base.ResetStats();
    }
}

public class MockMediaBackendA : MockMediaBackend;
public class MockMediaBackendB : MockMediaBackend;

public enum MockMediaBackendAction
{
    Nothing,
    Next,
    Pause,
    Play,
    PlayPause,
    Previous
}