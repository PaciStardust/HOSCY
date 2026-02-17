using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Translation.Core;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.TranslatorManagerServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public abstract class TranslatorManagerServiceTestBase<T> : TestBase<T>
{
    protected MockBackToFrontNotifyService _notify = null!;
    protected ConfigModel _config = null!;
    protected MockContainerBulkLoader<ITranslationModuleStartInfo> _infoLoader = null!;
    protected MockContainerBulkLoader<ITranslationModule> _moduleLoader = null!;

    protected MockTranslationModuleA _moduleA = null!;
    protected MockTranslationModuleB _moduleB = null!;

    protected MockTranslationModuleStartInfo _infoA = null!;
    protected MockTranslationModuleStartInfo _infoB = null!;
    protected MockTranslationModuleStartInfo _infoC = null!;

    protected TranslatorManagerService _translator = null!;

    protected void SetupSharedClasses()
    {
        _notify = new();
        _config = new();

        _moduleA = new();
        _moduleB = new();

        _infoA = new()
        {
            Name = "MockA",
            Description = "MockA",
            ModuleType = typeof(MockTranslationModuleA)
        };
        _infoB = new()
        {
            Name = "MockB",
            Description = "MockB",
            ModuleType = typeof(MockTranslationModuleB)
        };
        _infoC = new()
        {
            Name = "MockC",
            Description = "MockC",
            ModuleType = typeof(MockTranslationModuleStartInfo)
        };

        _infoLoader = new(() => [ _infoA, _infoB, _infoC ]);
        _moduleLoader = new(() => [ _moduleA, _moduleB ]);

        _translator = new(_config, _notify, _logger, _infoLoader, _moduleLoader);
    }

    protected void SetModule(string name)
    {
        _config.Translation_CurrentModule = name;
    }
    protected void SetAndRefreshModuleSelection(string name)
    {
        SetModule(name);
        _translator.RefreshModuleSelection();
    }
}

public class TranslatorManagerServiceStartupTests : TranslatorManagerServiceTestBase<TranslatorManagerServiceStartupTests>
{
    protected override void SetupExtra()
    {
        SetupSharedClasses();
    }

    [TestCase(false, false), TestCase(true, false), TestCase(false, true)]
    public void StartStopRestartTest(bool restartNotStart, bool doAgain)
    {
        SetModule("MockD");

        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_translator);
            Assert.That(_translator.GetModuleInfos(), Is.Empty);
            Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStarted(_translator);
            Assert.That(_translator.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        SetAndRefreshModuleSelection("MockA");

        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_translator);
            Assert.That(_translator.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }

        if (restartNotStart)
            _translator.Restart();
        else 
            _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_translator);
            Assert.That(_translator.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }
        
        _translator.Stop();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_translator);
            Assert.That(_translator.GetModuleInfos(), Is.Empty);
            Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        if (!doAgain) return;

        _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_translator);
            Assert.That(_translator.GetModuleInfos(), Has.Count.EqualTo(3));
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }
        
        _translator.Stop();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_translator);
            Assert.That(_translator.GetModuleInfos(), Is.Empty);
            Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
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

        _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.EqualTo(ex));
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
        }

        _moduleA.ExceptionToThrow = null;

        _translator.RefreshModuleSelection();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.Null);
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
        }
    }

    [Test]
    public void ThrowOnModuleStop()
    {
        SetModule(_infoA.Name);

        _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.Null);
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
        }

        var ex = new Exception();
        _moduleA.ExceptionToThrow = ex;

        SetAndRefreshModuleSelection(string.Empty);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.EqualTo(ex));
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
        }
    }

    [Test]
    public void ThrowOnModuleRestart()
    {
        SetModule(_infoA.Name);

        _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.Null);
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
        }

        var ex = new Exception();
        _moduleA.ExceptionToThrow = ex;

        _translator.RestartCurrentModule();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.EqualTo(ex));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
        }
    }
}

public class TranslatorManagerServiceFunctionTests : TranslatorManagerServiceTestBase<TranslatorManagerServiceFunctionTests>
{
    protected override void OneTimeSetupExtra()
    {
        SetupSharedClasses();
        _translator.Start();
    }

    protected override void SetupExtra()
    {
        SetAndRefreshModuleSelection(string.Empty);

        _config.Translation_SendUntranslatedIfFailed = false;
        _config.Translation_SkipLongerMessages = false;
        _config.Translation_MaxTextLength = 100;

        _notify.Notifications.Clear();

        _moduleA.ResetStats();
        _moduleB.ResetStats();
    }

    [Test]
    public void ModuleStartInfoLoadedTest()
    {
        SetModule(_infoA.Name);

        Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
        _translator.RefreshModuleSelection();

        var allModules = _translator.GetModuleInfos();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(allModules, Has.Count.EqualTo(3));
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
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
        Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
        AssertServiceStarted(_translator);

        SetAndRefreshModuleSelection(_infoA.Name);

        Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
        AssertServiceProcessing(_translator);

        SetAndRefreshModuleSelection(string.Empty);

        Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
        AssertServiceStarted(_translator);
    }

    [Test]
    public void GetModuleStatusTest()
    {
        var status = _translator.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped));
        
        SetAndRefreshModuleSelection(_infoA.Name);

        _moduleA.OverrideRunningStatus = ServiceStatus.Started;
        status = _translator.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Started));

        _moduleA.OverrideRunningStatus = ServiceStatus.Processing;
        status = _translator.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Processing));

        _moduleA.OverrideRunningStatus = ServiceStatus.Faulted;
        status = _translator.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Faulted));

        SetAndRefreshModuleSelection(string.Empty);

        status = _translator.GetCurrentModuleStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped));
    }

    [Test]
    public void RestartTest()
    {
        Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
        
        SetAndRefreshModuleSelection(_infoA.Name);
        Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));

        var stoppedA = false;

        void onAStopped(object? _, EventArgs __) { stoppedA = true; }
        _moduleA.OnModuleStopped += onAStopped;

        _translator.RestartCurrentModule();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
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
            Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_translator.GetCurrentModuleStatus(), Is.EqualTo(ServiceStatus.Stopped));

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
            Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_translator.GetCurrentModuleStatus(), Is.EqualTo(ServiceStatus.Stopped));

            Assert.That(_moduleA.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(stoppedA, Is.False);

            Assert.That(_moduleB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _translator.RefreshModuleSelection();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            Assert.That(_translator.GetCurrentModuleStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));

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
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            Assert.That(_translator.GetCurrentModuleStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));

            Assert.That(_moduleA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_moduleB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_moduleB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _translator.RefreshModuleSelection();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_translator.GetCurrentModuleStatus(), Is.EqualTo(ServiceStatus.Stopped));

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
        Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);

        SetAndRefreshModuleSelection(_infoC.Name);

        var ex = _translator.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex, Is.Not.Null);
            Assert.That(_translator.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
        }

        SetAndRefreshModuleSelection(string.Empty);

        var ex2 = _translator.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex2, Is.Null);
            Assert.That(_translator.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
        }
    }

    [Test]
    public void ManualShutdownHandledTest()
    {
        Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
        
        SetAndRefreshModuleSelection(_infoA.Name);

        Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));

        _moduleA.Stop();

        Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);

        _translator.RefreshModuleSelection();

        Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
    }

    [Test]
    public void HandleErrorTest()
    {
        Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);

        SetAndRefreshModuleSelection(_infoA.Name);

        Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));

        var testEx = new Exception("test");
        _moduleA.InduceError(testEx);

        var faultOutput = _translator.GetFaultIfExists();
        var faultHandler = _moduleA.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));

            Assert.That(faultOutput, Is.Not.Null);
            Assert.That(faultHandler, Is.Not.Null);
            Assert.That(faultOutput, Is.EqualTo(faultHandler));
        }

        _moduleA.InduceError(null);
        _translator.RefreshModuleSelection();

        faultOutput = _translator.GetFaultIfExists();
        faultHandler = _moduleA.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));

            Assert.That(faultOutput, Is.Null);
            Assert.That(faultHandler, Is.Null);
        }
    }

    [Test]
    public void ReceiverSwitchTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
            
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }

        SetAndRefreshModuleSelection(_infoA.Name);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoA));
            
            Assert.That(_moduleA.Started, Is.True);
            Assert.That(_moduleB.Started, Is.False);
        }

        SetAndRefreshModuleSelection(_infoB.Name);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentModuleInfo(), Is.EqualTo(_infoB));
            
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.True);
        }

        SetAndRefreshModuleSelection(string.Empty);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
            
            Assert.That(_moduleA.Started, Is.False);
            Assert.That(_moduleB.Started, Is.False);
        }
    }

    [TestCase(false), TestCase(true)]
    public void TranslatorUnavailableTest(bool untranslatedIfFailed)
    {
        _config.Translation_SendUntranslatedIfFailed = untranslatedIfFailed;

        Assert.That(_translator.GetCurrentModuleInfo(), Is.Null);
        
        var result = _translator.TryTranslate("Test", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_moduleA.ReceivedInput, Is.Empty);
            Assert.That(result, Is.EqualTo(untranslatedIfFailed ? TranslationResult.UseOriginal : TranslationResult.Failed));
            Assert.That(output, Is.Null);
        }

        SetAndRefreshModuleSelection(_infoA.Name);
        _moduleA.OverrideRunningStatus = ServiceStatus.Stopped;

        result = _translator.TryTranslate("Test", out output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_moduleA.ReceivedInput, Is.Empty);
            Assert.That(result, Is.EqualTo(untranslatedIfFailed ? TranslationResult.UseOriginal : TranslationResult.Failed));
            Assert.That(output, Is.Null);
        }

        _moduleA.OverrideRunningStatus = null;
        _moduleA.ReturnedOutput = "Waa";

        result = _translator.TryTranslate("Test", out output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_moduleA.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(result, Is.EqualTo(TranslationResult.Succeeded));
            Assert.That(output, Is.Not.Null);
        }
    }

    [TestCase(TranslationResult.Succeeded, false)]
    [TestCase(TranslationResult.UseOriginal, false)]
    [TestCase(TranslationResult.Failed, false)]
    [TestCase(TranslationResult.Failed, true)]
    public void TranslationTest(TranslationResult overrideResult, bool untranslatedIfFailed)
    {
        _config.Translation_SendUntranslatedIfFailed = untranslatedIfFailed;
        _moduleA.ReturnedOutput = "Echo";
        _moduleA.ReturnedResult = overrideResult;

        SetAndRefreshModuleSelection(_infoA.Name);
        
        var result = _translator.TryTranslate("Test", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_moduleA.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(_moduleB.ReceivedInput, Is.Empty);
            Assert.That(result, Is.EqualTo(untranslatedIfFailed ? TranslationResult.UseOriginal : overrideResult));
            Assert.That(output, overrideResult == TranslationResult.Succeeded ? Is.EqualTo("Echo") : Is.Null);
        }
        Assert.That(_moduleA.ReceivedInput[0], Is.EqualTo("Test"));
    }

    [TestCase(false), TestCase(true)]
    public void SkipLongerMessagesTest(bool untranslatedIfFailed)
    {
        _config.Translation_MaxTextLength = 10;
        _config.Translation_SkipLongerMessages = true;
        _config.Translation_SendUntranslatedIfFailed = untranslatedIfFailed;

        _moduleA.ReturnedOutput = "Echo";

        SetAndRefreshModuleSelection(_infoA.Name);

        var result = _translator.TryTranslate("0123456789", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_moduleA.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(result, Is.EqualTo(TranslationResult.Succeeded));
            Assert.That(output, Is.Not.Null);
        }

        result = _translator.TryTranslate("012345678901", out output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_moduleA.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(result, Is.EqualTo(untranslatedIfFailed ? TranslationResult.UseOriginal : TranslationResult.Failed));
            Assert.That(output, Is.Null);
        }
    }

    [TestCase("0123456789", "0123456789")]
    [TestCase("01234567890", "0123456789")]
    [TestCase("01234567 90123", "01234567")]
    public void CropLongerMessageTest(string input, string expectedOutput)
    {
        _config.Translation_MaxTextLength = 10;
        _config.Translation_SkipLongerMessages = false;

        _moduleA.ReturnedOutput = "Echo";

        SetAndRefreshModuleSelection(_infoA.Name);

        var result = _translator.TryTranslate(input, out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_moduleA.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(result, Is.EqualTo(TranslationResult.Succeeded));
            Assert.That(output, Is.EqualTo("Echo"));
        }
        Assert.That(_moduleA.ReceivedInput[0], Is.EqualTo(expectedOutput));
    }

    protected override void OneTimeTearDownExtra()
    {
        _translator.Stop();
    }
}