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
    protected MockContainerBulkLoader<ITranslationProviderStartInfo> _infoLoader = null!;
    protected MockContainerBulkLoader<ITranslationProvider> _providerLoader = null!;

    protected MockTranslationProviderA _providerA = null!;
    protected MockTranslationProviderB _providerB = null!;

    protected MockTranslationProviderStartInfo _infoA = null!;
    protected MockTranslationProviderStartInfo _infoB = null!;
    protected MockTranslationProviderStartInfo _infoC = null!;

    protected TranslatorManagerService _translator = null!;

    protected void SetupSharedClasses()
    {
        _notify = new();
        _config = new();

        _providerA = new();
        _providerB = new();

        _infoA = new()
        {
            Name = "MockA",
            Description = "MockA",
            ProviderType = typeof(MockTranslationProviderA)
        };
        _infoB = new()
        {
            Name = "MockB",
            Description = "MockB",
            ProviderType = typeof(MockTranslationProviderB)
        };
        _infoC = new()
        {
            Name = "MockC",
            Description = "MockC",
            ProviderType = typeof(MockTranslationProviderStartInfo)
        };

        _infoLoader = new(() => [ _infoA, _infoB, _infoC ]);
        _providerLoader = new(() => [ _providerA, _providerB ]);

        _translator = new(_notify, _logger, _config, _infoLoader, _providerLoader);
    }

    protected void SetProvider(string name)
    {
        _config.Translation_CurrentProvider = name;
    }
    protected void SetAndRefreshProvider(string name)
    {
        SetProvider(name);
        _translator.RefreshProvider();
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
        SetProvider("MockD");

        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_translator);
            Assert.That(_translator.GetProviderInfos(), Is.Empty);
            Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
            Assert.That(_providerA.Started, Is.False);
            Assert.That(_providerB.Started, Is.False);
        }

        _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStarted(_translator);
            Assert.That(_translator.GetProviderInfos(), Has.Count.EqualTo(3));
            Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
            Assert.That(_providerA.Started, Is.False);
            Assert.That(_providerB.Started, Is.False);
        }

        SetAndRefreshProvider("MockA");

        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_translator);
            Assert.That(_translator.GetProviderInfos(), Has.Count.EqualTo(3));
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
            Assert.That(_providerA.Started, Is.True);
            Assert.That(_providerB.Started, Is.False);
        }

        if (restartNotStart)
            _translator.Restart();
        else 
            _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_translator);
            Assert.That(_translator.GetProviderInfos(), Has.Count.EqualTo(3));
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
            Assert.That(_providerA.Started, Is.True);
            Assert.That(_providerB.Started, Is.False);
        }
        
        _translator.Stop();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_translator);
            Assert.That(_translator.GetProviderInfos(), Is.Empty);
            Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
            Assert.That(_providerA.Started, Is.False);
            Assert.That(_providerB.Started, Is.False);
        }

        if (!doAgain) return;

        _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_translator);
            Assert.That(_translator.GetProviderInfos(), Has.Count.EqualTo(3));
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
            Assert.That(_providerA.Started, Is.True);
            Assert.That(_providerB.Started, Is.False);
        }
        
        _translator.Stop();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_translator);
            Assert.That(_translator.GetProviderInfos(), Is.Empty);
            Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
            Assert.That(_providerA.Started, Is.False);
            Assert.That(_providerB.Started, Is.False);
        }
    }

    [Test]
    public void ThrowOnProviderStart()
    {
        var ex = new Exception();
        _providerA.ExceptionToThrow = ex;
        SetProvider(_infoA.Name);

        _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.EqualTo(ex));
            Assert.That(_providerA.Started, Is.False);
            Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
        }

        _providerA.ExceptionToThrow = null;

        _translator.RefreshProvider();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.Null);
            Assert.That(_providerA.Started, Is.True);
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
        }
    }

    [Test]
    public void ThrowOnProviderStop()
    {
        SetProvider(_infoA.Name);

        _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.Null);
            Assert.That(_providerA.Started, Is.True);
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
        }

        var ex = new Exception();
        _providerA.ExceptionToThrow = ex;

        SetAndRefreshProvider(string.Empty);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.EqualTo(ex));
            Assert.That(_providerA.Started, Is.False);
            Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
        }
    }

    [Test]
    public void ThrowOnProviderRestart()
    {
        SetProvider(_infoA.Name);

        _translator.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.Null);
            Assert.That(_providerA.Started, Is.True);
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
        }

        var ex = new Exception();
        _providerA.ExceptionToThrow = ex;

        _translator.RestartCurrentProvider();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_translator.GetFaultIfExists(), Is.EqualTo(ex));
            Assert.That(_providerA.Started, Is.True);
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
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
        SetAndRefreshProvider(string.Empty);

        _config.Translation_SendUntranslatedIfFailed = false;
        _config.Translation_SkipLongerMessages = false;
        _config.Translation_MaxTextLength = 100;

        _notify.Notifications.Clear();

        _providerA.ResetStats();
        _providerB.ResetStats();
    }

    [Test]
    public void ProviderStartInfoLoadedTest()
    {
        SetProvider(_infoA.Name);

        Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
        _translator.RefreshProvider();

        var allProviders = _translator.GetProviderInfos();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(allProviders, Has.Count.EqualTo(3));
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
            Assert.That(_providerA.Started, Is.True);
            Assert.That(_providerB.Started, Is.False);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(allProviders, Does.Contain(_infoA));
            Assert.That(allProviders, Does.Contain(_infoB));
            Assert.That(allProviders, Does.Contain(_infoC));
        }
    }

    [Test]
    public void GetStatusTest()
    {
        Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
        AssertServiceStarted(_translator);

        SetAndRefreshProvider(_infoA.Name);

        Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
        AssertServiceProcessing(_translator);

        SetAndRefreshProvider(string.Empty);

        Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
        AssertServiceStarted(_translator);
    }

    [Test]
    public void GetProviderStatusTest()
    {
        var status = _translator.GetCurrentProviderStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped));
        
        SetAndRefreshProvider(_infoA.Name);

        _providerA.OverrideRunningStatus = ServiceStatus.Started;
        status = _translator.GetCurrentProviderStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Started));

        _providerA.OverrideRunningStatus = ServiceStatus.Processing;
        status = _translator.GetCurrentProviderStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Processing));

        _providerA.OverrideRunningStatus = ServiceStatus.Faulted;
        status = _translator.GetCurrentProviderStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Faulted));

        SetAndRefreshProvider(string.Empty);

        status = _translator.GetCurrentProviderStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped));
    }

    [Test]
    public void RestartTest()
    {
        Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
        
        SetAndRefreshProvider(_infoA.Name);
        Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));

        var stoppedA = false;

        void onAStopped(object? _, EventArgs __) { stoppedA = true; }
        _providerA.OnSubmoduleStopped += onAStopped;

        _translator.RestartCurrentProvider();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
            Assert.That(stoppedA, Is.True);
        }

        _providerA.OnSubmoduleStopped -= onAStopped;
    }

    [Test]
    public void RefreshTest()
    {
        var stoppedA = false;
        var stoppedB = false;

        void onAStopped(object? _, EventArgs __) { stoppedA = true; }
        _providerA.OnSubmoduleStopped += onAStopped;
        void onBStopped(object? _, EventArgs __) { stoppedB = true; }
        _providerB.OnSubmoduleStopped += onBStopped;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
            Assert.That(_translator.GetCurrentProviderStatus(), Is.EqualTo(ServiceStatus.Stopped));

            Assert.That(_providerA.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_providerA.Started, Is.False);
            Assert.That(stoppedA, Is.False);

            Assert.That(_providerB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_providerB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        SetProvider(_infoA.Name);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
            Assert.That(_translator.GetCurrentProviderStatus(), Is.EqualTo(ServiceStatus.Stopped));

            Assert.That(_providerA.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_providerA.Started, Is.False);
            Assert.That(stoppedA, Is.False);

            Assert.That(_providerB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_providerB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _translator.RefreshProvider();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
            Assert.That(_translator.GetCurrentProviderStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));

            Assert.That(_providerA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_providerA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_providerB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_providerB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        SetProvider(string.Empty);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
            Assert.That(_translator.GetCurrentProviderStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));

            Assert.That(_providerA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_providerA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_providerB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_providerB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _translator.RefreshProvider();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
            Assert.That(_translator.GetCurrentProviderStatus(), Is.EqualTo(ServiceStatus.Stopped));

            Assert.That(_providerA.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_providerA.Started, Is.False);
            Assert.That(stoppedA, Is.True);

            Assert.That(_providerB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_providerB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _providerA.OnSubmoduleStopped -= onAStopped;
        _providerB.OnSubmoduleStopped -= onBStopped;
    }

    [Test] 
    public void RefreshBrokenTest()
    {
        Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);

        SetAndRefreshProvider(_infoC.Name);

        var ex = _translator.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex, Is.Not.Null);
            Assert.That(_translator.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
        }

        SetAndRefreshProvider(string.Empty);

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
        Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
        
        SetAndRefreshProvider(_infoA.Name);

        Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));

        _providerA.Stop();

        Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);

        _translator.RefreshProvider();

        Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
    }

    [Test]
    public void HandleErrorTest()
    {
        Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);

        SetAndRefreshProvider(_infoA.Name);

        Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));

        var testEx = new Exception("test");
        _providerA.InduceError(testEx);

        var faultOutput = _translator.GetFaultIfExists();
        var faultHandler = _providerA.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));

            Assert.That(faultOutput, Is.Not.Null);
            Assert.That(faultHandler, Is.Not.Null);
            Assert.That(faultOutput, Is.EqualTo(faultHandler));
        }

        _providerA.InduceError(null);
        _translator.RefreshProvider();

        faultOutput = _translator.GetFaultIfExists();
        faultHandler = _providerA.GetFaultIfExists();
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
            Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
            
            Assert.That(_providerA.Started, Is.False);
            Assert.That(_providerB.Started, Is.False);
        }

        SetAndRefreshProvider(_infoA.Name);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoA));
            
            Assert.That(_providerA.Started, Is.True);
            Assert.That(_providerB.Started, Is.False);
        }

        SetAndRefreshProvider(_infoB.Name);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentProviderInfo(), Is.EqualTo(_infoB));
            
            Assert.That(_providerA.Started, Is.False);
            Assert.That(_providerB.Started, Is.True);
        }

        SetAndRefreshProvider(string.Empty);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
            
            Assert.That(_providerA.Started, Is.False);
            Assert.That(_providerB.Started, Is.False);
        }
    }

    [TestCase(false), TestCase(true)]
    public void TranslatorUnavailableTest(bool untranslatedIfFailed)
    {
        _config.Translation_SendUntranslatedIfFailed = untranslatedIfFailed;

        Assert.That(_translator.GetCurrentProviderInfo(), Is.Null);
        
        var result = _translator.TryTranslate("Test", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_providerA.ReceivedInput, Is.Empty);
            Assert.That(result, Is.EqualTo(untranslatedIfFailed ? TranslationResult.UseOriginal : TranslationResult.Failed));
            Assert.That(output, Is.Null);
        }

        SetAndRefreshProvider(_infoA.Name);
        _providerA.OverrideRunningStatus = ServiceStatus.Stopped;

        result = _translator.TryTranslate("Test", out output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_providerA.ReceivedInput, Is.Empty);
            Assert.That(result, Is.EqualTo(untranslatedIfFailed ? TranslationResult.UseOriginal : TranslationResult.Failed));
            Assert.That(output, Is.Null);
        }

        _providerA.OverrideRunningStatus = null;
        _providerA.ReturnedOutput = "Waa";

        result = _translator.TryTranslate("Test", out output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_providerA.ReceivedInput, Has.Count.EqualTo(1));
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
        _providerA.ReturnedOutput = "Echo";
        _providerA.ReturnedResult = overrideResult;

        SetAndRefreshProvider(_infoA.Name);
        
        var result = _translator.TryTranslate("Test", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_providerA.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(_providerB.ReceivedInput, Is.Empty);
            Assert.That(result, Is.EqualTo(untranslatedIfFailed ? TranslationResult.UseOriginal : overrideResult));
            Assert.That(output, overrideResult == TranslationResult.Succeeded ? Is.EqualTo("Echo") : Is.Null);
        }
        Assert.That(_providerA.ReceivedInput[0], Is.EqualTo("Test"));
    }

    [TestCase(false), TestCase(true)]
    public void SkipLongerMessagesTest(bool untranslatedIfFailed)
    {
        _config.Translation_MaxTextLength = 10;
        _config.Translation_SkipLongerMessages = true;
        _config.Translation_SendUntranslatedIfFailed = untranslatedIfFailed;

        _providerA.ReturnedOutput = "Echo";

        SetAndRefreshProvider(_infoA.Name);

        var result = _translator.TryTranslate("0123456789", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_providerA.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(result, Is.EqualTo(TranslationResult.Succeeded));
            Assert.That(output, Is.Not.Null);
        }

        result = _translator.TryTranslate("012345678901", out output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_providerA.ReceivedInput, Has.Count.EqualTo(1));
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

        _providerA.ReturnedOutput = "Echo";

        SetAndRefreshProvider(_infoA.Name);

        var result = _translator.TryTranslate(input, out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_providerA.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(result, Is.EqualTo(TranslationResult.Succeeded));
            Assert.That(output, Is.EqualTo("Echo"));
        }
        Assert.That(_providerA.ReceivedInput[0], Is.EqualTo(expectedOutput));
    }

    protected override void OneTimeTearDownExtra()
    {
        _translator.Stop();
    }
}