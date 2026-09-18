using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Media.Core;
using HoscyCore.Services.Translation.Core;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Impl;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.MediaControlServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public abstract class MediaControlServiceTestBase<T> : SoloModuleManagerTestBase
<
    T,
    IMediaBackendStartInfo,
    MockMediaBackendStartInfo,
    IMediaBackend,
    MockMediaBackendA,
    MockMediaBackendB,
    MediaControlService
> 
{
    protected ConfigModel _config = null!;

    protected override MediaControlService CreateController()
    {
        return new MediaControlService(_notify, _logger, _infoLoader, _moduleLoader, _config);
    }

    protected override void SetupSharedClassesExtra()
    {
        _config = new();
    }

    protected override void SetModule(string name)
    {
        _config.Media_Backend = name;
    }
}

public class MediaControlServiceFunctionTests : MediaControlServiceTestBase<MediaControlServiceFunctionTests>
{
    protected override void OneTimeSetupExtra()
    {
        SetupSharedClasses();
        _manager.Start().AssertOk();
    }

    protected override void SetupExtra()
    {
        SetModule(string.Empty);
        _manager.StopModule().AssertOk();

        _notify.Notifications.Clear();

        _moduleA.ResetStats();
        _moduleB.ResetStats();
    }

    [TestCase(true, false), TestCase(false, false), TestCase(false, true)]
    public void TestActions(bool start, bool error)
    {
        if (start)
        {
            SetModule(_infoA.Name);
            _manager.StartModule().AssertOk();

            var module = _manager.GetCurrentModuleInfo();

            using (Assert.EnterMultipleScope()) 
            {
                Assert.That(module, Is.Not.Null);
                Assert.That(module?.IsOk, Is.True);
                Assert.That(module?.Value, Is.EqualTo(_infoA));
                Assert.That(_moduleA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            }
        }

        _moduleA.DoThrowNext = error;
        var res = _manager.PlayAsync().AsSync();
        AssertAction(MockMediaBackendAction.Play);
        _moduleA.ResetStats();

        _moduleA.DoThrowNext = error;
        res = _manager.PauseAsync().AsSync();
        AssertAction(MockMediaBackendAction.Pause);
        _moduleA.ResetStats();

        _moduleA.DoThrowNext = error;
        res = _manager.PlayPauseAsync().AsSync();
        AssertAction(MockMediaBackendAction.PlayPause);
        _moduleA.ResetStats();
        
        _moduleA.DoThrowNext = error;
        res = _manager.NextAsync().AsSync();
        AssertAction(MockMediaBackendAction.Next);
        _moduleA.ResetStats();

        _moduleA.DoThrowNext = error;
        res = _manager.PreviousAsync().AsSync();
        AssertAction(MockMediaBackendAction.Previous);
        _moduleA.ResetStats();

        void AssertAction(MockMediaBackendAction action)
        {
            if (!start || error)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(res.IsOk, Is.Not.True);
                    Assert.That(_moduleA.LastReceivedAction, Is.EqualTo(MockMediaBackendAction.Nothing));
                }
            }
            else
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(res.IsOk, Is.True);
                    Assert.That(_moduleA.LastReceivedAction, Is.EqualTo(action));
                }
            }
        }
    }

    protected override void OneTimeTearDownExtra()
    {
        _manager.Stop().AssertOk();
    }
}