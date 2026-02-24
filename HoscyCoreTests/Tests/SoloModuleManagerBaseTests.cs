using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Translation.Core;
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
    SoloModuleManagerBase<ITranslationModuleStartInfo, ITranslationModule, TranslationManagerService>
>
{
    protected ConfigModel _config = null!;

    protected override SoloModuleManagerBase<ITranslationModuleStartInfo, ITranslationModule, TranslationManagerService> CreateController()
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

        _manager.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStarted(_manager);
            Assert.That(_manager.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        SetModule("MockA");
        _manager.StartModule();

        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_manager);
            Assert.That(_manager.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }

        if (restartNotStart)
            _manager.Restart();
        else 
            _manager.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_manager);
            Assert.That(_manager.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }
        
        _manager.Stop();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_manager);
            Assert.That(_manager.GetModuleInfos(), Is.Empty);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        if (!doAgain) return;

        _manager.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_manager);
            Assert.That(_manager.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }
        
        _manager.Stop();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_manager);
            Assert.That(_manager.GetModuleInfos(), Is.Empty);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }
    }

    [Test]
    public void ThrowOnModuleStart()
    {
        var ex = new Exception();
        _moduleA.ExceptionToThrow = ex;
        SetModule(_infoA.Name);

        _manager.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetFaultIfExists(), Is.EqualTo(ex));
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        }

        _moduleA.ExceptionToThrow = null;
        _manager.StartModule();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetFaultIfExists(), Is.Null);
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
        }
    }

    [Test]
    public void ThrowOnModuleStop()
    {
        SetModule(_infoA.Name);

        _manager.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetFaultIfExists(), Is.Null);
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
        }

        var ex = new Exception();
        _moduleA.ExceptionToThrow = ex;

        _manager.StopModule();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetFaultIfExists(), Is.EqualTo(ex));
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        }
    }

    [Test]
    public void ThrowOnModuleRestart()
    {
        SetModule(_infoA.Name);

        _manager.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetFaultIfExists(), Is.Null);
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
        }

        var ex = new Exception();
        _moduleA.ExceptionToThrow = ex;

        _manager.RestartModule();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_manager.GetFaultIfExists(), Is.EqualTo(ex));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
        }
    }

    [TestCase(false), TestCase(true)]
    public void AutomaticModuleStartTest(bool automaticStart)
    {
        SetModule(_infoA.Name);
        _config.Translation_AutoStart = automaticStart;

        _manager.Start();

        using (Assert.EnterMultipleScope())
        {
            if (automaticStart)
            {
                AssertServiceProcessing(_manager);
                Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
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

        _manager.StartModule();

        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_manager);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }
    }
}

public class StartStopModuleControllerBaseFunctionTests : SoloModuleManagerBaseTestBase<StartStopModuleControllerBaseFunctionTests>
{
    protected override void OneTimeSetupExtra()
    {
        SetupSharedClasses();
        _manager.Start();
    }

    protected override void SetupExtra()
    {
        _manager.StopModule();
        SetModule(string.Empty);

        _notify.Notifications.Clear();

        _moduleA.ResetStats();
        _moduleB.ResetStats();
    }

    [Test]
    public void ModuleStartInfoLoadedTest()
    {
        SetModule(_infoA.Name);

        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        _manager.StartModule();

        var allModules = _manager.GetModuleInfos();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(allModules, Has.Count.EqualTo(3));
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
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
        _manager.StartModule();

        Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
        AssertServiceProcessing(_manager);

        _manager.StopModule();

        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);

        SetModule(string.Empty);
        _manager.StartModule();

        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);

        AssertServiceStarted(_manager);
    }

    [Test]
    public void GetModuleStatusTest()
    {
        var status = _manager.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped));
        
        SetModule(_infoA.Name);
        _manager.StartModule();

        _moduleA.OverrideRunningStatus = ServiceStatus.Started;
        status = _manager.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Started));

        _moduleA.OverrideRunningStatus = ServiceStatus.Processing;
        status = _manager.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Processing));

        _moduleA.OverrideRunningStatus = ServiceStatus.Faulted;
        status = _manager.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Faulted));

        _manager.StopModule();

        status = _manager.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped));
    }

    [Test]
    public void RestartTest()
    {
        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        
        SetModule(_infoA.Name);
        _manager.StartModule();
        Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));

        var stoppedA = false;

        void onAStopped(object? _, EventArgs __) { stoppedA = true; }
        _moduleA.OnModuleStopped += onAStopped;

        _manager.RestartModule();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
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

        _manager.StartModule();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
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
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            Assert.That(_manager.GetCurrentModuleStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));

            Assert.That(_moduleA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_moduleB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _manager.StartModule();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            Assert.That(_manager.GetCurrentModuleStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));

            Assert.That(_moduleA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_moduleB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _manager.StopModule();
        _manager.StartModule();

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
        _manager.StartModule();

        var ex = _manager.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex, Is.Not.Null);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
        }

        _manager.StopModule();

        var ex2 = _manager.GetFaultIfExists();
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
        _manager.StartModule();

        Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));

        _moduleA.Stop();

        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);

        _manager.StartModule();

        Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
    }

    [Test]
    public void HandleErrorTest()
    {
        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);

        SetModule(_infoA.Name);
        _manager.StartModule();

        Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));

        var testEx = new Exception("test");
        _moduleA.InduceError(testEx);

        var faultOutput = _manager.GetFaultIfExists();
        var faultHandler = _moduleA.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));

            Assert.That(faultOutput, Is.Not.Null);
            Assert.That(faultHandler, Is.Not.Null);
            Assert.That(faultOutput, Is.EqualTo(faultHandler));
        }

        _moduleA.InduceError(null);
        _manager.StopModule();
        _manager.StartModule();

        faultOutput = _manager.GetFaultIfExists();
        faultHandler = _moduleA.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));

            Assert.That(faultOutput, Is.Null);
            Assert.That(faultHandler, Is.Null);
        }
    }

    [TestCase]
    public void ModuleSwitchTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        SetModule(_infoA.Name);
        _manager.StartModule();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }

        SetModule(_infoB.Name);
        _manager.StartModule();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }

        _manager.StopModule();
        _manager.StartModule();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoB));
            
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.True);
        }

        SetModule(string.Empty);
        _manager.StopModule();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        _manager.StartModule();

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
            _manager.StartModule();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_manager.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
                Assert.That(_moduleA.Started, Is.True);
                Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Processing));
            }
        }

        for (var i = 0; i < 2; i++)
        {
            _manager.StopModule();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
                Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started));
            }
        }

        _manager.RestartModule();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_manager.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started));
        }
    }

    protected override void OneTimeTearDownExtra()
    {
        _manager.Stop();
    }
}