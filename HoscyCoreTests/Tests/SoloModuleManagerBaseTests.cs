using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Translation.Core;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Impl;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.StartStopModuleControllerBaseTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public abstract class SoloModuleManagerBaseTestBase<T> : SoloModuleManagerTestBase 
<
    T, 
    ITranslationModuleStartInfo, 
    MockTranslationModuleStartInfo, 
    ITranslationModule, 
    MockTranslationModuleA, 
    MockTranslationModuleB, 
    SoloModuleManagerBase<ITranslationModuleStartInfo, ITranslationModule>
>
{
    protected ConfigModel _config = null!;

    protected override SoloModuleManagerBase<ITranslationModuleStartInfo, ITranslationModule> CreateController()
    {
        // We are using the translator as it does not really matter
        return new TranslationManagerService(_config, _notify, _logger, _infoLoader, _moduleLoader);
    }

    protected override void SetupSharedClassesExtra()
    {
        _config = new();
    }

    protected override void SetModule(string name)
    {
        _config.Translation_SelectedModuleName = name;
    }
}

public class StartStopModuleControllerBaseStartupTests : SoloModuleManagerBaseTestBase<StartStopModuleControllerBaseStartupTests>
{
    protected override void SetupExtra()
    {
        SetupSharedClasses();
        _config.Translation_AutoStart = true;
    }

    [TestCase(false, false), TestCase(true, false), TestCase(false, true)]
    public void StartStopRestartTest(bool restartNotStart, bool doAgain)
    {
        SetModule(string.Empty);

        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_manager);
            Assert.That(_manager.GetModuleInfos(), Is.Empty);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        _manager.Start().AssertOk();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStarted(_manager);
            Assert.That(_manager.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        SetModule("MockA");
        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_manager);
            Assert.That(_manager.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }

        if (restartNotStart)
            _manager.Restart().AssertOk();
        else 
            _manager.Start().AssertOk();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_manager);
            Assert.That(_manager.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }
        
        _manager.Stop().AssertOk();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_manager);
            Assert.That(_manager.GetModuleInfos(), Is.Empty);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        if (!doAgain) return;

        _manager.Start().AssertOk();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_manager);
            Assert.That(_manager.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }
        
        _manager.Stop().AssertOk();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_manager);
            Assert.That(_manager.GetModuleInfos(), Is.Empty);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }
    }

    [TestCase(false), TestCase(true)]
    public void AutomaticModuleStartTest(bool automaticStart)
    {
        SetModule(_infoA.Name);
        _config.Translation_AutoStart = automaticStart;

        _manager.Start().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            if (automaticStart)
            {
                AssertServiceProcessing(_manager);
                Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
                Assert.That(_moduleA.Started, Is.True);
            }
            else
            {
                AssertServiceStarted(_manager);
                Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
                Assert.That(_moduleA.Started, Is.False);
            }
            Assert.That(_moduleB.Started, Is.False);
        }

        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_manager);
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }
    }

    [Test]
    public void NoModuleInfosOnStartTest()
    {
        _infoLoader.InstanceGenerator = () => [];

        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        _manager.Start().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_manager.GetErrorMessageIfExists(), Is.Not.Null);
        }
    }

    [Test]
    public void AutomaticModuleStartFailTest()
    {
        _config.Translation_AutoStart = true;
        var ex = ResMsg.Err("Module error");

        SetModule(_infoA.Name);
        _moduleA.ResultToReturn = ResC.Fail(ex);

        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        _manager.Start().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetErrorMessageIfExists(), Is.Not.Null);
        }
        Assert.That(_manager.GetErrorMessageIfExists().Message, Does.Contain(ex.Message));
    }
}

public class StartStopModuleControllerBaseFunctionTests : SoloModuleManagerBaseTestBase<StartStopModuleControllerBaseFunctionTests>
{
    protected override void OneTimeSetupExtra()
    {
        SetupSharedClasses();
        _manager.Start().AssertOk();
    }

    protected override void SetupExtra()
    {
        _moduleA.ResetStats();
        _moduleB.ResetStats();

        _manager.StopModule().AssertOk();
        SetModule(string.Empty);
        _manager.StartModule().AssertOk();

        _notify.Notifications.Clear();

        _moduleA.ResetStats();
        _moduleB.ResetStats();
    }

    [Test]
    public void ModuleStartInfoLoadedTest()
    {
        SetModule(_infoA.Name);

        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        _manager.StartModule().AssertOk();

        var allModules = _manager.GetModuleInfos();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(allModules, Has.Count.EqualTo(3));
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(allModules, Does.Contain(_infoA));
            Assert.That(allModules, Does.Contain(_infoB));
            Assert.That(allModules, Does.Contain(_infoC));
        }
    }

    [Test]
    public void GetStatusTest()
    {
        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        AssertServiceStarted(_manager);

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
        AssertServiceProcessing(_manager);

        _manager.StopModule().AssertOk();

        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);

        SetModule(string.Empty);
        _manager.StartModule().AssertOk();

        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);

        AssertServiceStarted(_manager);
    }

    [Test]
    public void GetModuleStatusTest()
    {
        var status = _manager.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped));
        
        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        _moduleA.OverrideRunningStatus = ServiceStatus.Started;
        status = _manager.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Started));

        _moduleA.OverrideRunningStatus = ServiceStatus.Processing;
        status = _manager.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Processing));

        _moduleA.OverrideRunningStatus = ServiceStatus.Faulted;
        status = _manager.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Faulted));

        _manager.StopModule().AssertOk();

        status = _manager.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped));
    }

    [Test]
    public void RestartTest()
    {
        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        
        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();
        Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));

        var stoppedA = false;

        void onAStopped(object? _, EventArgs __) { stoppedA = true; }
        _moduleA.OnModuleStopped += onAStopped;

        _manager.RestartModule().AssertOk();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            Assert.That(stoppedA, Is.True);
        }

        _moduleA.OnModuleStopped -= onAStopped;
    }

    [Test]
    public void RefreshTest()
    {
        var stoppedA = false;
        var stoppedB = false;

        void onAStopped(object? _, EventArgs __) { stoppedA = true; }
        _moduleA.OnModuleStopped += onAStopped;
        void onBStopped(object? _, EventArgs __) { stoppedB = true; }
        _moduleB.OnModuleStopped += onBStopped;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_manager.GetCurrentModuleStatus(), Is.EqualTo(ServiceStatus.Stopped));

            Assert.That(_moduleA.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(stoppedA, Is.False);

            Assert.That(_moduleB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        SetModule(_infoA.Name);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_manager.GetCurrentModuleStatus(), Is.EqualTo(ServiceStatus.Stopped));

            Assert.That(_moduleA.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(stoppedA, Is.False);

            Assert.That(_moduleB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            Assert.That(_manager.GetCurrentModuleStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));

            Assert.That(_moduleA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_moduleB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        SetModule(string.Empty);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            Assert.That(_manager.GetCurrentModuleStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));

            Assert.That(_moduleA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_moduleB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            Assert.That(_manager.GetCurrentModuleStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));

            Assert.That(_moduleA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_moduleB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _manager.StopModule().AssertOk();
        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_manager.GetCurrentModuleStatus(), Is.EqualTo(ServiceStatus.Stopped));

            Assert.That(_moduleA.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(stoppedA, Is.True);

            Assert.That(_moduleB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _moduleA.OnModuleStopped -= onAStopped;
        _moduleB.OnModuleStopped -= onBStopped;
    }

    [Test] 
    public void RefreshBrokenTest()
    {
        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);

        SetModule(_infoC.Name);
        _manager.StartModule().AssertFail();

        var ex = _manager.GetErrorMessageIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex, Is.Not.Null);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
        }

        _manager.StopModule().AssertOk();

        var ex2 = _manager.GetErrorMessageIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex2, Is.Null);
            Assert.That(_manager.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
        }
    }

    [Test]
    public void ManualShutdownHandledTest()
    {
        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        
        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));

        _moduleA.Stop().AssertOk();

        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);

        _manager.StartModule().AssertOk();

        Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
    }

    [Test]
    public void HandleErrorTest()
    {
        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));

        var testEx = ResMsg.Err("test");
        _moduleA.InduceError(testEx);

        var faultOutput = _manager.GetErrorMessageIfExists();
        var faultHandler = _moduleA.GetErrorMessageIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));

            Assert.That(faultOutput, Is.Not.Null);
            Assert.That(faultHandler, Is.Not.Null);
            Assert.That(faultOutput, Is.EqualTo(faultHandler));
        }

        _moduleA.InduceError(null);
        _manager.StopModule().AssertOk();
        _manager.StartModule().AssertOk();

        faultOutput = _manager.GetErrorMessageIfExists();
        faultHandler = _moduleA.GetErrorMessageIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));

            Assert.That(faultOutput, Is.Null);
            Assert.That(faultHandler, Is.Null);
        }
    }

    [Test]
    public void ModuleSwitchTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }

        SetModule(_infoB.Name);
        _manager.StartModule().AssertFail();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }

        _manager.StopModule().AssertOk();
        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoB));
            
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.True);
        }

        SetModule(string.Empty);
        _manager.StopModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }
    }

    [Test]
    public void DoubleActionsTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started));
        }

        SetModule(_infoA.Name);
        for (var i = 0; i < 2; i++)
        {
            _manager.StartModule().AssertOk();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
                Assert.That(_moduleA.Started, Is.True);
                Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Processing));
            }
        }

        for (var i = 0; i < 2; i++)
        {
            _manager.StopModule().AssertOk();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
                Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started));
            }
        }

        _manager.RestartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started));
        }
    }

    [Test]
    public void UnexpectedModuleStopTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started));
            Assert.That(_manager.GetErrorMessageIfExists(), Is.Null);
            Assert.That(_notify.Notifications, Is.Empty);
        }

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Processing));
            Assert.That(_manager.GetErrorMessageIfExists(), Is.Null);
            Assert.That(_notify.Notifications, Is.Empty);
        }

        _moduleA.Stop().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetErrorMessageIfExists(), Is.Not.Null);
            Assert.That(_notify.Notifications, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void ThrowOnModuleRestartTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started));
            Assert.That(_manager.GetErrorMessageIfExists(), Is.Null);
        }

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetErrorMessageIfExists(), Is.Null);
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
        }

        var ex = ResC.Fail(ResMsg.Err("This is a test"));
        _moduleA.ResultToReturn = ex;

        _manager.RestartModule().AssertFail();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetErrorMessageIfExists()?.Message, Does.Contain(ex.Msg!.Message));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
        }
    }

    [Test]
    public void ThrowOnModuleStopTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started));
            Assert.That(_manager.GetErrorMessageIfExists(), Is.Null);
        }

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetErrorMessageIfExists(), Is.Null);
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
        }

        var ex = ResC.Fail(ResMsg.Err("This is a test"));
        _moduleA.ResultToReturn = ex;

        _manager.StopModule().AssertFail();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetErrorMessageIfExists()?.Message, Does.Contain(ex.Msg!.Message));
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        }
    }

    [Test] 
    public void ThrowOnModuleStartTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started));
            Assert.That(_manager.GetErrorMessageIfExists(), Is.Null);
        }

        var ex = ResC.Fail(ResMsg.Err("This is a test"));
        _moduleA.ResultToReturn = ex;
        SetModule(_infoA.Name);
        _manager.StartModule().AssertFail();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetErrorMessageIfExists()?.Message, Does.Contain(ex.Msg!.Message));
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        }

        _moduleA.ResultToReturn = null;
        _manager.StartModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetErrorMessageIfExists(), Is.Null);
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_manager.GetCurrentModuleInfo()?.Value, Is.EqualTo(_infoA));
        }
    }

    protected override void OneTimeTearDownExtra()
    {
        _manager.Stop().AssertOk();
    }
}