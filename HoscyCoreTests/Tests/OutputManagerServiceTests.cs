using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Translation.Core;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.OutputManagerServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public abstract class OutputManagerServiceTestBase<T> : TestBase<T>
{
    protected MockBackToFrontNotifyService _notify = null!;
    protected MockTranslatorManager _translator = null!;
    protected ConfigModel _config = null!;

    protected MockContainerBulkLoader<IOutputPreprocessor> _loadPreprocessors = null!;
    protected MockOutputPreprocessor _preprocessorEarlyFull = null!;
    protected MockOutputPreprocessor _preprocessorLatePartial = null!;

    protected MockContainerBulkLoader<IOutputHandlerStartInfo> _loadHandlerInfos = null!;
    protected MockOutputHandlerStartInfo _infoA = null!;
    protected MockOutputHandlerStartInfo _infoB = null!;
    protected MockOutputHandlerStartInfo _infoC = null!;
    protected MockOutputHandlerStartInfo _infoD = null!;
    protected MockOutputHandlerStartInfo _infoE = null!;

    protected MockContainerBulkLoader<IOutputHandler> _loadHandlers = null!;
    protected MockOutputHandlerA _handlerA = null!;
    protected MockOutputHandlerB _handlerB = null!;
    protected MockOutputHandlerC _handlerC = null!;
    protected MockOutputHandlerD _handlerD = null!;

    protected OutputManagerService _output = null!;

    protected void SetSharedClasses()
    {
        _config = new();
        _translator = new();
        _notify = new();

        _preprocessorEarlyFull = new()
        {
            HandlingStage = OutputPreprocessorHandlingStage.Initial,
            ContinueIfHandled = false,
            FullReplace = true
        };
        _preprocessorLatePartial = new()
        {
            HandlingStage = OutputPreprocessorHandlingStage.Final,
            ContinueIfHandled = true,
            FullReplace = false
        };
        _loadPreprocessors = new(() => [ _preprocessorEarlyFull, _preprocessorLatePartial ]);

        _handlerA = new()
        {
            Name = "HandlerA",
            OutputTypeFlags = OutputsAsMediaFlags.OutputsAsText,
            TranslationFormat = OutputTranslationFormat.Both
        };
        _handlerB = new()
        {
            Name = "HandlerB",
            OutputTypeFlags = OutputsAsMediaFlags.OutputsAsAudio,
            TranslationFormat = OutputTranslationFormat.Translation
        };
        _handlerC = new()
        {
            Name = "HandlerC",
            OutputTypeFlags = OutputsAsMediaFlags.OutputsAsOther,
            TranslationFormat = OutputTranslationFormat.Untranslated
        };
        _handlerD = new()
        {
            Name = "HandlerD",
            OutputTypeFlags = OutputsAsMediaFlags.OutputsAsText | OutputsAsMediaFlags.OutputsAsAudio | OutputsAsMediaFlags.OutputsAsOther,
            TranslationFormat = OutputTranslationFormat.Untranslated
        };
        _loadHandlers = new(() => [ _handlerA, _handlerB, _handlerC, _handlerD ]);

        _infoA = new()
        {
            HandlerType = typeof(MockOutputHandlerA),
            Enabled = true,
        };
        _infoB = new()
        {
            HandlerType = typeof(MockOutputHandlerB),
        };
        _infoC = new()
        {
            HandlerType = typeof(MockOutputHandlerC),
        };
        _infoD = new()
        {
            HandlerType = typeof(MockOutputHandlerD),
        };
        _infoE = new()
        {
            HandlerType = typeof(MockOutputHandlerStartInfo),
        };
        _loadHandlerInfos = new(() => [ _infoA, _infoB, _infoC, _infoD, _infoE ]);

        _output = new(_logger, _notify, _config, _loadPreprocessors, _loadHandlerInfos, _loadHandlers, _translator);
    }
}

public class OutputManagerServiceStartupTests : OutputManagerServiceTestBase<OutputManagerServiceStartupTests>
{
    protected override void SetupExtra()
    {
        SetSharedClasses();
    }

    [TestCase(false, false), TestCase(true, false), TestCase(false, true)]
    public void StartStopRestartTest(bool restartNotStart, bool doAgain)
    {
        _infoA.Enabled = false;

        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_output);
            Assert.That(_output.GetHandlerInfos(false), Is.Empty);
            Assert.That(_output.GetHandlerInfos(true), Is.Empty);
            Assert.That(_handlerA.Started, Is.False);
            Assert.That(_handlerB.Started, Is.False);
        }

        _output.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStarted(_output);
            Assert.That(_output.GetHandlerInfos(false), Has.Count.EqualTo(5));
            Assert.That(_output.GetHandlerInfos(true), Is.Empty);
            Assert.That(_handlerA.Started, Is.False);
            Assert.That(_handlerB.Started, Is.False);
        }

        _infoA.Enabled = true;
        _output.RefreshHandlers();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_output);
            Assert.That(_output.GetHandlerInfos(false), Has.Count.EqualTo(5));
            var activeHandlers = _output.GetHandlerInfos(true);
            Assert.That(activeHandlers, Has.Count.EqualTo(1));
            Assert.That(activeHandlers, Does.Contain(_infoA));
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(_handlerB.Started, Is.False);
        }

        if (restartNotStart)
            _output.Restart();
        else
            _output.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_output);
            Assert.That(_output.GetHandlerInfos(false), Has.Count.EqualTo(5));
            var activeHandlers = _output.GetHandlerInfos(true);
            Assert.That(activeHandlers, Has.Count.EqualTo(1));
            Assert.That(activeHandlers, Does.Contain(_infoA));
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(_handlerB.Started, Is.False);
        }

        _output.Stop();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_output);
            Assert.That(_output.GetHandlerInfos(false), Is.Empty);
            Assert.That(_output.GetHandlerInfos(true), Is.Empty);
            Assert.That(_handlerA.Started, Is.False);
            Assert.That(_handlerB.Started, Is.False);
        }

        if (!doAgain) return;

        _output.Start();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_output);
            Assert.That(_output.GetHandlerInfos(false), Has.Count.EqualTo(5));
            var activeHandlers = _output.GetHandlerInfos(true);
            Assert.That(activeHandlers, Has.Count.EqualTo(1));
            Assert.That(activeHandlers, Does.Contain(_infoA));
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(_handlerB.Started, Is.False);
        }

        _output.Stop();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStopped(_output);
            Assert.That(_output.GetHandlerInfos(false), Is.Empty);
            Assert.That(_output.GetHandlerInfos(true), Is.Empty);
            Assert.That(_handlerA.Started, Is.False);
            Assert.That(_handlerB.Started, Is.False);
        }
    }

    [Test]
    public void ThrowOnHandlerStart()
    {
        var ex = new Exception();
        _handlerA.ExceptionToThrow = ex;

        _output.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_output.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_output.GetFaultIfExists(), Is.EqualTo(ex));
            Assert.That(_handlerA.Started, Is.False);
            Assert.That(_output.GetHandlerInfos(true), Is.Empty);
        }

        _handlerA.ExceptionToThrow = null;

        _output.RefreshHandlers();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_output.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_output.GetFaultIfExists(), Is.Null);
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(_output.GetHandlerInfos(true), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void ThrowOnHandlerStop()
    {
        _output.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_output.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_output.GetFaultIfExists(), Is.Null);
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(_output.GetHandlerInfos(true), Has.Count.EqualTo(1));
        }

        var ex = new Exception();
        _handlerA.ExceptionToThrow = ex;

        _infoA.Enabled = false;

        _output.RefreshHandlers();
        var fault = _output.GetFaultIfExists() as CombinedException;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_output.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(fault, Is.Not.Null);
            Assert.That(_handlerA.Started, Is.False);
            Assert.That(_output.GetHandlerInfos(true), Is.Empty);
        }
        Assert.That(fault!.Exceptions, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fault.Exceptions[0], Is.EqualTo(ex));
            Assert.That(fault.Exceptions[1], Is.EqualTo(ex));
        }
    }

    [Test]
    public void ThrowOnHandlerRestart()
    {
        _output.Start();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_output.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
            Assert.That(_output.GetFaultIfExists(), Is.Null);
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(_output.GetHandlerInfos(true), Has.Count.EqualTo(1));
        }

        var ex = new Exception();
        _handlerA.ExceptionToThrow = ex;

        _output.RestartHandlers();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_output.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
            Assert.That(_output.GetFaultIfExists(), Is.EqualTo(ex));
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(_output.GetHandlerInfos(true), Has.Count.EqualTo(1));
        }
    }
}

public class OutputManagerServiceFunctionTests : OutputManagerServiceTestBase<OutputManagerServiceFunctionTests>
{
    protected override void OneTimeSetupExtra()
    {
        SetSharedClasses();
        _output.Start();
    }

    protected override void SetupExtra()
    {
        _preprocessorEarlyFull.Enabled = false;
        _preprocessorEarlyFull.ProcessedOutput = null;
        _preprocessorEarlyFull.ContinueIfHandled = true;

        _preprocessorLatePartial.Enabled = false;
        _preprocessorLatePartial.ProcessedOutput = null;
        _preprocessorLatePartial.ContinueIfHandled = true;

        _infoA.Enabled = false;
        _infoB.Enabled = false;
        _infoC.Enabled = false;
        _infoD.Enabled = false;
        _infoE.Enabled = false;

        _output.RefreshHandlers();
        Assert.That(_output.GetHandlerInfos(true), Is.Empty);

        _translator.ReceivedInput.Clear();
        _translator.CurrentProviderStatus = ServiceStatus.Processing;
        _notify.Notifications.Clear();

        _preprocessorEarlyFull.ReceivedInput.Clear();
        _preprocessorLatePartial.ReceivedInput.Clear();

        _handlerA.ResetStats();
        _handlerB.ResetStats();
        _handlerC.ResetStats();
        _handlerD.ResetStats();
    }

    [Test]
    public void HandlerStartInfoLoadedTest()
    {
        _infoA.Enabled = true;

        var preRefreshHandlers = _output.GetHandlerInfos(true);
        _output.RefreshHandlers();

        var allHandlers = _output.GetHandlerInfos(false);
        var activeHandlers = _output.GetHandlerInfos(true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(allHandlers, Has.Count.EqualTo(5));
            Assert.That(preRefreshHandlers, Is.Empty);
            Assert.That(activeHandlers, Has.Count.EqualTo(1));
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(_handlerB.Started, Is.False);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(allHandlers, Does.Contain(_infoA));
            Assert.That(allHandlers, Does.Contain(_infoB));
            Assert.That(allHandlers, Does.Contain(_infoC));
            Assert.That(allHandlers, Does.Contain(_infoD));
            Assert.That(allHandlers, Does.Contain(_infoE));

            Assert.That(activeHandlers, Does.Contain(_infoA));
        }
    }

    [Test]
    public void GetStatusTest()
    {
        Assert.That(_output.GetHandlerInfos(true), Is.Empty);
        AssertServiceStarted(_output);

        _infoA.Enabled = true;
        _output.RefreshHandlers();

        Assert.That(_output.GetHandlerInfos(true), Has.Count.EqualTo(1));
        AssertServiceProcessing(_output);

        _infoA.Enabled = false;
        _output.RefreshHandlers();

        Assert.That(_output.GetHandlerInfos(true), Is.Empty);
        AssertServiceStarted(_output);
    }

    [Test]
    public void GetHandlerStatusTest()
    {
        var status = _output.GetProcessorStatus(_infoA);
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped));
        
        _infoA.Enabled = true;
        _output.RefreshHandlers();

        _handlerA.OverrideRunningStatus = ServiceStatus.Started;
        status = _output.GetProcessorStatus(_infoA);
        Assert.That(status, Is.EqualTo(ServiceStatus.Started));

        _handlerA.OverrideRunningStatus = ServiceStatus.Processing;
        status = _output.GetProcessorStatus(_infoA);
        Assert.That(status, Is.EqualTo(ServiceStatus.Processing));

        _handlerA.OverrideRunningStatus = ServiceStatus.Faulted;
        status = _output.GetProcessorStatus(_infoA);
        Assert.That(status, Is.EqualTo(ServiceStatus.Faulted));

        _infoA.Enabled = false;
        _output.RefreshHandlers();

        status = _output.GetProcessorStatus(_infoA);
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped));
    }

    [Test]
    public void RestartTest()
    {
        Assert.That(_output.GetHandlerInfos(true), Is.Empty);
        _infoA.Enabled = true;
        _infoB.Enabled = true;

        _output.RefreshHandlers();
        Assert.That(_output.GetHandlerInfos(true), Has.Count.EqualTo(2));

        var stoppedA = false;
        var stoppedB = false;

        void onAStopped(object? _, EventArgs __) { stoppedA = true; }
        _handlerA.OnModuleStopped += onAStopped;
        void onBStopped(object? _, EventArgs __) { stoppedB = true; }
        _handlerB.OnModuleStopped += onBStopped;

        _output.RestartHandlers();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_output.GetHandlerInfos(true), Has.Count.EqualTo(2));
            Assert.That(stoppedA, Is.True);
            Assert.That(stoppedB, Is.True);
        }

        _handlerA.OnModuleStopped -= onAStopped;
        _handlerB.OnModuleStopped -= onBStopped;
    }

    [Test]
    public void RefreshTest()
    {
        var stoppedA = false;
        var stoppedB = false;

        void onAStopped(object? _, EventArgs __) { stoppedA = true; }
        _handlerA.OnModuleStopped += onAStopped;
        void onBStopped(object? _, EventArgs __) { stoppedB = true; }
        _handlerB.OnModuleStopped += onBStopped;

        using (Assert.EnterMultipleScope())
        {
            var activeHandlers = _output.GetHandlerInfos(true);

            Assert.That(activeHandlers, Is.Empty);

            Assert.That(_handlerA.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoA), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Not.Contain(_infoA));
            Assert.That(_handlerA.Started, Is.False);
            Assert.That(stoppedA, Is.False);

            Assert.That(_handlerB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoB), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Not.Contain(_infoB));
            Assert.That(_handlerB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _infoA.Enabled = true;
        _infoB.Enabled = true;

        using (Assert.EnterMultipleScope())
        {
            var activeHandlers = _output.GetHandlerInfos(true);

            Assert.That(activeHandlers, Is.Empty);

            Assert.That(_handlerA.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoA), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Not.Contain(_infoA));
            Assert.That(_handlerA.Started, Is.False);
            Assert.That(stoppedA, Is.False);

            Assert.That(_handlerB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoB), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Not.Contain(_infoB));
            Assert.That(_handlerB.Started, Is.False);
            Assert.That(stoppedB, Is.False);
        }

        _output.RefreshHandlers();

        using (Assert.EnterMultipleScope())
        {
            var activeHandlers = _output.GetHandlerInfos(true);

            Assert.That(activeHandlers, Has.Count.EqualTo(2));

            Assert.That(_handlerA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoA), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Contain(_infoA));
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_handlerB.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoB), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Contain(_infoB));
            Assert.That(_handlerB.Started, Is.True);
            Assert.That(stoppedB, Is.False);
        }

        _infoB.Enabled = false;

        using (Assert.EnterMultipleScope())
        {
            var activeHandlers = _output.GetHandlerInfos(true);

            Assert.That(activeHandlers, Has.Count.EqualTo(2));

            Assert.That(_handlerA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoA), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Contain(_infoA));
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_handlerB.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoB), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Contain(_infoB));
            Assert.That(_handlerB.Started, Is.True);
            Assert.That(stoppedB, Is.False);
        }

        _output.RefreshHandlers();

        using (Assert.EnterMultipleScope())
        {
            var activeHandlers = _output.GetHandlerInfos(true);

            Assert.That(activeHandlers, Has.Count.EqualTo(1));

            Assert.That(_handlerA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoA), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Contain(_infoA));
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_handlerB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoB), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Not.Contain(_infoB));
            Assert.That(_handlerB.Started, Is.False);
            Assert.That(stoppedB, Is.True);
        }

        _infoA.Enabled = false;

        using (Assert.EnterMultipleScope())
        {
            var activeHandlers = _output.GetHandlerInfos(true);

            Assert.That(activeHandlers, Has.Count.EqualTo(1));

            Assert.That(_handlerA.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoA), Is.Not.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Contain(_infoA));
            Assert.That(_handlerA.Started, Is.True);
            Assert.That(stoppedA, Is.False);

            Assert.That(_handlerB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoB), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Not.Contain(_infoB));
            Assert.That(_handlerB.Started, Is.False);
            Assert.That(stoppedB, Is.True);
        }

        _output.RefreshHandlers();

        using (Assert.EnterMultipleScope())
        {
            var activeHandlers = _output.GetHandlerInfos(true);

            Assert.That(activeHandlers, Is.Empty);

            Assert.That(_handlerA.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoA), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Not.Contain(_infoA));
            Assert.That(_handlerA.Started, Is.False);
            Assert.That(stoppedA, Is.True);

            Assert.That(_handlerB.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(_output.GetProcessorStatus(_infoB), Is.EqualTo(ServiceStatus.Stopped));
            Assert.That(activeHandlers, Does.Not.Contain(_infoB));
            Assert.That(_handlerB.Started, Is.False);
            Assert.That(stoppedB, Is.True);
        }

        _handlerA.OnModuleStopped -= onAStopped;
        _handlerB.OnModuleStopped -= onBStopped;
    }

    [Test] 
    public void RefreshBrokenTest()
    {
        Assert.That(_output.GetHandlerInfos(true), Is.Empty);

        _infoE.Enabled = true;
        _output.RefreshHandlers();

        var ex = _output.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex, Is.Not.Null);
            Assert.That(_output.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));
        }

        _infoE.Enabled = false;
        _output.RefreshHandlers();

        var ex2 = _output.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex2, Is.Null);
            Assert.That(_output.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));
        }
    }

    [Test]
    public void ManualShutdownHandledTest()
    {
        Assert.That(_output.GetHandlerInfos(true), Is.Empty);
        
        _infoA.Enabled = true;
        _output.RefreshHandlers();

        Assert.That(_output.GetHandlerInfos(true), Has.Count.EqualTo(1));

        _handlerA.Stop();

        Assert.That(_output.GetHandlerInfos(true), Is.Empty);

        _output.RefreshHandlers();

        Assert.That(_output.GetHandlerInfos(true), Has.Count.EqualTo(1));
    }

    [Test]
    public void HandleErrorTest()
    {
        Assert.That(_output.GetHandlerInfos(true), Is.Empty);

        _infoA.Enabled = true;
        _output.RefreshHandlers();

        Assert.That(_output.GetHandlerInfos(true), Has.Count.EqualTo(1));

        var testEx = new Exception("test");
        _handlerA.InduceError(testEx);

        var faultOutput = _output.GetFaultIfExists();
        var faultHandler = _handlerA.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_output.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Faulted));

            Assert.That(faultOutput, Is.Not.Null);
            Assert.That(faultHandler, Is.Not.Null);
        }

        var faultOutputConverted = faultOutput;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(faultOutputConverted, Is.Not.Null);
            Assert.That(faultHandler, Is.EqualTo(testEx));
        }
        Assert.That(faultOutputConverted, Is.EqualTo(testEx));

        _handlerA.InduceError(null);
        _output.RefreshHandlers();

        faultOutput = _output.GetFaultIfExists();
        faultHandler = _handlerA.GetFaultIfExists();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_output.GetCurrentStatus(), Is.Not.EqualTo(ServiceStatus.Faulted));

            Assert.That(faultOutput, Is.Null);
            Assert.That(faultHandler, Is.Null);
        }
    }

    [Test]
    public void ProcessingIndicatorTest()
    {
        bool? lastIndicator = null;

        _infoA.Enabled = true;
        _infoB.Enabled = true;
        void onIndicator(object? _, bool x) { lastIndicator = x; }
        _output.OnProcessingIndicatorSet += onIndicator;
        _output.RefreshHandlers();
        
        _output.SetProcessingIndicator(true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handlerA.ReceivedIndicatorStates, Has.Count.EqualTo(1));
            Assert.That(_handlerB.ReceivedIndicatorStates, Has.Count.EqualTo(1));
            Assert.That(_handlerC.ReceivedIndicatorStates, Is.Empty);
            Assert.That(lastIndicator, Is.True);
        }
        Assert.That(_handlerA.ReceivedIndicatorStates[0], Is.True);

        _infoB.Enabled = false;
        _output.RefreshHandlers();

        _output.SetProcessingIndicator(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handlerA.ReceivedIndicatorStates, Has.Count.EqualTo(2));
            Assert.That(_handlerB.ReceivedIndicatorStates, Has.Count.EqualTo(1));
            Assert.That(_handlerC.ReceivedIndicatorStates, Is.Empty);
            Assert.That(lastIndicator, Is.False);
        }
        Assert.That(_handlerA.ReceivedIndicatorStates[1], Is.False);

        _output.OnProcessingIndicatorSet -= onIndicator;
    }

    [Test]
    public void ClearTest()
    {
        var clears = 0;

        _infoA.Enabled = true;
        _infoB.Enabled = true;
        void onClear(object? _, EventArgs __) { clears++; }
        _output.OnClear += onClear;
        _output.RefreshHandlers();
        
        _output.Clear();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handlerA.ClearCount, Is.EqualTo(1));
            Assert.That(_handlerB.ClearCount, Is.EqualTo(1));
            Assert.That(_handlerC.ClearCount, Is.EqualTo(0));
            Assert.That(clears, Is.EqualTo(1));
        }

        _infoB.Enabled = false;
        _output.RefreshHandlers();

        _output.Clear();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handlerA.ClearCount, Is.EqualTo(2));
            Assert.That(_handlerB.ClearCount, Is.EqualTo(2)); // Will still go up because clear on stop
            Assert.That(_handlerC.ClearCount, Is.EqualTo(0));
            Assert.That(clears, Is.EqualTo(2));
        }

        _output.Clear();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handlerA.ClearCount, Is.EqualTo(3));
            Assert.That(_handlerB.ClearCount, Is.EqualTo(2));
            Assert.That(_handlerC.ClearCount, Is.EqualTo(0));
            Assert.That(clears, Is.EqualTo(3));
        }

        _output.OnClear -= onClear;
    }

    [Test]
    public void SendSimpleTest()
    {
        List<OutputNotificationEventArgs> notificationsFromEvent = [];
        List<OutputMessageEventArgs> messagesFromEvent = [];

        void onNotification(object? _, OutputNotificationEventArgs y) => notificationsFromEvent.Add(y);
        _output.OnNotification += onNotification;
        void onMessage(object? _, OutputMessageEventArgs y) { messagesFromEvent.Add(y); }
        _output.OnMessage += onMessage;

        _infoA.Enabled = true;
        _infoB.Enabled = true;
        _infoC.Enabled = true;
        _output.RefreshHandlers();

        _output.SendMessage("TestMessage", OutputSettingsFlags.AllowAllOutputs);
        _output.SendNotification("TestNotification", OutputNotificationPriority.Critical, OutputSettingsFlags.AllowAllOutputs);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handlerA.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_handlerB.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_handlerC.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(messagesFromEvent, Has.Count.EqualTo(1));

            Assert.That(_handlerA.ReceivedNotifications, Has.Count.EqualTo(1));
            Assert.That(_handlerB.ReceivedNotifications, Has.Count.EqualTo(1));
            Assert.That(_handlerC.ReceivedNotifications, Has.Count.EqualTo(1));
            Assert.That(notificationsFromEvent, Has.Count.EqualTo(1));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handlerA.ReceivedMessages[0], Is.EqualTo("TestMessage"));
            Assert.That(_handlerB.ReceivedMessages[0], Is.EqualTo("TestMessage"));
            Assert.That(_handlerC.ReceivedMessages[0], Is.EqualTo("TestMessage"));

            Assert.That(_handlerA.ReceivedNotifications[0].Message, Is.EqualTo("TestNotification"));
            Assert.That(_handlerB.ReceivedNotifications[0].Message, Is.EqualTo("TestNotification"));
            Assert.That(_handlerC.ReceivedNotifications[0].Message, Is.EqualTo("TestNotification"));

            Assert.That(_handlerA.ReceivedNotifications[0].Priority, Is.EqualTo(OutputNotificationPriority.Critical));
            Assert.That(_handlerB.ReceivedNotifications[0].Priority, Is.EqualTo(OutputNotificationPriority.Critical));
            Assert.That(_handlerC.ReceivedNotifications[0].Priority, Is.EqualTo(OutputNotificationPriority.Critical));

            Assert.That(messagesFromEvent[0].Contents, Is.EqualTo("TestMessage"));
            Assert.That(messagesFromEvent[0].Translation, Is.Null);
            Assert.That(messagesFromEvent[0].Outputs, Has.Length.EqualTo(3));
            Assert.That(messagesFromEvent[0].Outputs, Does.Contain(_handlerA.Name).And.Contain(_handlerB.Name).And.Contain(_handlerC.Name));

            Assert.That(notificationsFromEvent[0].Contents, Is.EqualTo("TestNotification"));
            Assert.That(notificationsFromEvent[0].Priority, Is.EqualTo(OutputNotificationPriority.Critical));
            Assert.That(notificationsFromEvent[0].Outputs, Has.Length.EqualTo(3));
            Assert.That(notificationsFromEvent[0].Outputs, Does.Contain(_handlerA.Name).And.Contain(_handlerB.Name).And.Contain(_handlerC.Name));
        }

        _infoB.Enabled = false;
        _output.RefreshHandlers();

        _handlerA.ResetStats();
        _handlerB.ResetStats();
        _handlerC.ResetStats();

        notificationsFromEvent.Clear();
        messagesFromEvent.Clear();

        _output.SendMessage("TestMessage2", OutputSettingsFlags.AllowAllOutputs);
        _output.SendNotification("TestNotification2", OutputNotificationPriority.Minimal, OutputSettingsFlags.AllowAllOutputs);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handlerA.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_handlerB.ReceivedMessages, Is.Empty);
            Assert.That(_handlerC.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(messagesFromEvent, Has.Count.EqualTo(1));

            Assert.That(_handlerA.ReceivedNotifications, Has.Count.EqualTo(1));
            Assert.That(_handlerB.ReceivedNotifications, Is.Empty);
            Assert.That(_handlerC.ReceivedNotifications, Has.Count.EqualTo(1));
            Assert.That(notificationsFromEvent, Has.Count.EqualTo(1));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handlerA.ReceivedMessages[0], Is.EqualTo("TestMessage2"));
            Assert.That(_handlerC.ReceivedMessages[0], Is.EqualTo("TestMessage2"));

            Assert.That(_handlerA.ReceivedNotifications[0].Message, Is.EqualTo("TestNotification2"));
            Assert.That(_handlerC.ReceivedNotifications[0].Message, Is.EqualTo("TestNotification2"));

            Assert.That(_handlerA.ReceivedNotifications[0].Priority, Is.EqualTo(OutputNotificationPriority.Minimal));
            Assert.That(_handlerC.ReceivedNotifications[0].Priority, Is.EqualTo(OutputNotificationPriority.Minimal));

            Assert.That(messagesFromEvent[0].Contents, Is.EqualTo("TestMessage2"));
            Assert.That(messagesFromEvent[0].Translation, Is.Null);
            Assert.That(messagesFromEvent[0].Outputs, Has.Length.EqualTo(2));
            Assert.That(messagesFromEvent[0].Outputs, Does.Contain(_handlerA.Name).And.Not.Contain(_handlerB.Name).And.Contain(_handlerC.Name));

            Assert.That(notificationsFromEvent[0].Contents, Is.EqualTo("TestNotification2"));
            Assert.That(notificationsFromEvent[0].Priority, Is.EqualTo(OutputNotificationPriority.Minimal));
            Assert.That(notificationsFromEvent[0].Outputs, Has.Length.EqualTo(2));
            Assert.That(notificationsFromEvent[0].Outputs, Does.Contain(_handlerA.Name).And.Not.Contain(_handlerB.Name).And.Contain(_handlerC.Name));
        }

        _output.OnNotification -= onNotification;
        _output.OnMessage -= onMessage;
    }

    [Test]
    public void PreprocessorFilterTest()
    {
        _preprocessorEarlyFull.Enabled = true;
        _preprocessorLatePartial.Enabled = true;

        for (var i = 0; i < 0b100; i++)
        {
            _preprocessorEarlyFull.ReceivedInput.Clear();
            _preprocessorLatePartial.ReceivedInput.Clear();

            var flags = ((i & 0b010) > 0 ? OutputSettingsFlags.DoPreprocessFull : OutputSettingsFlags.None)
                | ((i & 0b001) > 0 ? OutputSettingsFlags.DoPreprocessPartial : OutputSettingsFlags.None);

            var messageText = "Message" + i;
            var notificationText = "Notification" + i;

            _output.SendMessage(messageText, flags);
            _output.SendNotification(notificationText, OutputNotificationPriority.Critical, flags);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_preprocessorEarlyFull.ReceivedInput,
                    Has.Count.EqualTo(flags.HasFlag(OutputSettingsFlags.DoPreprocessFull) ? 2 : 0), i.ToString());

                Assert.That(_preprocessorLatePartial.ReceivedInput,
                    Has.Count.EqualTo(flags.HasFlag(OutputSettingsFlags.DoPreprocessPartial) ? 2 : 0), i.ToString());
            }

            using (Assert.EnterMultipleScope())
            {
                if (flags.HasFlag(OutputSettingsFlags.DoPreprocessFull))
                {
                    Assert.That(_preprocessorEarlyFull.ReceivedInput, Does.Contain(notificationText), i.ToString());
                    Assert.That(_preprocessorEarlyFull.ReceivedInput, Does.Contain(messageText), i.ToString());
                }
                
                if (flags.HasFlag(OutputSettingsFlags.DoPreprocessPartial))
                {
                    Assert.That(_preprocessorLatePartial.ReceivedInput, Does.Contain(notificationText), i.ToString());
                    Assert.That(_preprocessorLatePartial.ReceivedInput, Does.Contain(messageText), i.ToString());
                }
            }
        }
    }

    [Test]
    public void HandlerFilterTest()
    {
        _infoA.Enabled = true;
        _infoB.Enabled = true;
        _infoC.Enabled = true;
        _infoD.Enabled = true;

        _output.RefreshHandlers();

        for (var i = 0; i < 0b1000; i++)
        {
            _handlerA.ResetStats();
            _handlerB.ResetStats();
            _handlerC.ResetStats();
            _handlerD.ResetStats();

            var flags = ((i & 0b0100) > 0 ? OutputSettingsFlags.AllowTextOutput : OutputSettingsFlags.None)
                | ((i & 0b0010) > 0 ? OutputSettingsFlags.AllowAudioOutput : OutputSettingsFlags.None)
                | ((i & 0b0001) > 0 ? OutputSettingsFlags.AllowOtherOutput : OutputSettingsFlags.None);

            var messageText = "Message" + i;
            var notificationText = "Notification" + i;

            _output.SendMessage(messageText, flags);
            _output.SendNotification(notificationText, OutputNotificationPriority.Critical, flags);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_handlerA.ReceivedMessages,
                    Has.Count.EqualTo(flags.HasFlag(OutputSettingsFlags.AllowTextOutput) ? 1 : 0), i.ToString());
                Assert.That(_handlerA.ReceivedNotifications,
                    Has.Count.EqualTo(flags.HasFlag(OutputSettingsFlags.AllowTextOutput) ? 1 : 0), i.ToString());

                Assert.That(_handlerB.ReceivedMessages,
                    Has.Count.EqualTo(flags.HasFlag(OutputSettingsFlags.AllowAudioOutput) ? 1 : 0), i.ToString());
                Assert.That(_handlerB.ReceivedNotifications,
                    Has.Count.EqualTo(flags.HasFlag(OutputSettingsFlags.AllowAudioOutput) ? 1 : 0), i.ToString());

                Assert.That(_handlerC.ReceivedMessages,
                    Has.Count.EqualTo(flags.HasFlag(OutputSettingsFlags.AllowOtherOutput) ? 1 : 0), i.ToString());
                Assert.That(_handlerC.ReceivedNotifications,
                    Has.Count.EqualTo(flags.HasFlag(OutputSettingsFlags.AllowOtherOutput) ? 1 : 0), i.ToString());

                Assert.That(_handlerD.ReceivedMessages,
                    Has.Count.EqualTo(flags != OutputSettingsFlags.None ? 1 : 0), i.ToString());
                Assert.That(_handlerD.ReceivedNotifications,
                    Has.Count.EqualTo(flags != OutputSettingsFlags.None ? 1 : 0), i.ToString());
            }

            using (Assert.EnterMultipleScope())
            {
                if (flags.HasFlag(OutputSettingsFlags.AllowTextOutput))
                {
                    Assert.That(_handlerA.ReceivedMessages[0], Is.EqualTo(messageText), i.ToString());
                    Assert.That(_handlerA.ReceivedNotifications[0].Message, Is.EqualTo(notificationText), i.ToString());
                }
                
                if (flags.HasFlag(OutputSettingsFlags.AllowAudioOutput))
                {
                    Assert.That(_handlerB.ReceivedMessages[0], Is.EqualTo(messageText), i.ToString());
                    Assert.That(_handlerB.ReceivedNotifications[0].Message, Is.EqualTo(notificationText), i.ToString());
                }

                if (flags.HasFlag(OutputSettingsFlags.AllowOtherOutput))
                {
                    Assert.That(_handlerC.ReceivedMessages[0], Is.EqualTo(messageText), i.ToString());
                    Assert.That(_handlerC.ReceivedNotifications[0].Message, Is.EqualTo(notificationText), i.ToString());
                }

                if (flags != OutputSettingsFlags.None)
                {
                    Assert.That(_handlerD.ReceivedMessages[0], Is.EqualTo(messageText), i.ToString());
                    Assert.That(_handlerD.ReceivedNotifications[0].Message, Is.EqualTo(notificationText), i.ToString());
                }
            }
        }
    }

    [Test]
    public void TranslationFormatTest()
    {
        _translator.TranslateOutput = "TlSuccess";
        _translator.TranslateResult = TranslationResult.Succeeded;

        List<OutputNotificationEventArgs> notificationsFromEvent = [];
        List<OutputMessageEventArgs> messagesFromEvent = [];

        void onMessage(object? _, OutputMessageEventArgs y) { messagesFromEvent.Add(y); }
        _output.OnMessage += onMessage;

        _infoA.Enabled = true;
        _infoB.Enabled = true;
        _infoC.Enabled = true;
        _output.RefreshHandlers();

        var flags = OutputSettingsFlags.AllowAllOutputs | OutputSettingsFlags.DoTranslate;
        _output.SendMessage("MsgTest", flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messagesFromEvent, Has.Count.EqualTo(1));
            Assert.That(_translator.ReceivedInput, Has.Count.EqualTo(1));

            Assert.That(_handlerA.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_handlerB.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_handlerC.ReceivedMessages, Has.Count.EqualTo(1));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messagesFromEvent[0].Contents, Is.EqualTo("MsgTest"));
            Assert.That(messagesFromEvent[0].Translation, Is.EqualTo("TlSuccess"));

            Assert.That(_translator.ReceivedInput[0], Is.EqualTo("MsgTest"));

            Assert.That(_handlerA.ReceivedMessages[0], Does.Contain("MsgTest").And.Contain("TlSuccess"));
            Assert.That(_handlerB.ReceivedMessages[0], Is.EqualTo("TlSuccess"));
            Assert.That(_handlerC.ReceivedMessages[0], Is.EqualTo("MsgTest"));
        }

        _handlerA.ResetStats();
        _handlerB.ResetStats();
        _handlerC.ResetStats();

        _translator.ReceivedInput.Clear();
        messagesFromEvent.Clear();

        flags = OutputSettingsFlags.AllowAllOutputs;
        _output.SendMessage("MsgTest2", flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messagesFromEvent, Has.Count.EqualTo(1));
            Assert.That(_translator.ReceivedInput, Is.Empty);

            Assert.That(_handlerA.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_handlerB.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_handlerC.ReceivedMessages, Has.Count.EqualTo(1));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messagesFromEvent[0].Contents, Is.EqualTo("MsgTest2"));
            Assert.That(messagesFromEvent[0].Translation, Is.Null);

            Assert.That(_handlerA.ReceivedMessages[0], Is.EqualTo("MsgTest2"));
            Assert.That(_handlerB.ReceivedMessages[0], Is.EqualTo("MsgTest2"));
            Assert.That(_handlerC.ReceivedMessages[0], Is.EqualTo("MsgTest2"));
        }

        _output.OnMessage -= onMessage;
    }

    [Test]
    public void TranslationResultTest()
    {
        _translator.TranslateOutput = "TlResult";
        _translator.TranslateResult = TranslationResult.Succeeded;

        _infoB.Enabled = true;
        _output.RefreshHandlers();

        var flags = OutputSettingsFlags.AllowAllOutputs | OutputSettingsFlags.DoTranslate;

        _output.SendMessage("Message1", flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(_handlerB.ReceivedMessages, Has.Count.EqualTo(1));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.ReceivedInput[0], Is.EqualTo("Message1"));
            Assert.That(_handlerB.ReceivedMessages[0], Is.EqualTo("TlResult"));
        }

        _translator.TranslateResult = TranslationResult.UseOriginal;

        _output.SendMessage("Message2", flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.ReceivedInput, Has.Count.EqualTo(2));
            Assert.That(_handlerB.ReceivedMessages, Has.Count.EqualTo(2));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.ReceivedInput[1], Is.EqualTo("Message2"));
            Assert.That(_handlerB.ReceivedMessages[1], Is.EqualTo("Message2"));
        }

        _translator.TranslateResult = TranslationResult.Failed;

        _output.SendMessage("Message3", flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.ReceivedInput, Has.Count.EqualTo(3));
            Assert.That(_handlerB.ReceivedMessages, Has.Count.EqualTo(2));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.ReceivedInput[2], Is.EqualTo("Message3"));
        }
    }

    [Test]
    public void TranslatorUnavailableTest()
    {
        _config.Translation_SendUntranslatedIfUnavailable = false;
        _translator.TranslateOutput = "Output";
        _translator.TranslateResult = TranslationResult.Succeeded;
        _translator.CurrentProviderStatus = ServiceStatus.Stopped;

        _infoB.Enabled = true;
        _infoC.Enabled = true;
        _output.RefreshHandlers();

        var flags = OutputSettingsFlags.AllowAllOutputs | OutputSettingsFlags.DoTranslate;

        _output.SendMessage("Test1", flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(_handlerB.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_handlerC.ReceivedMessages, Has.Count.EqualTo(1));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.ReceivedInput[0], Is.EqualTo("Test1"));
            Assert.That(_handlerB.ReceivedMessages[0], Is.EqualTo("Output"));
            Assert.That(_handlerC.ReceivedMessages[0], Is.EqualTo("Test1"));
        }

        _infoB.Enabled = false;
        _output.RefreshHandlers();

        _output.SendMessage("Test2", flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(_handlerB.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_handlerC.ReceivedMessages, Has.Count.EqualTo(1));
        }

        _config.Translation_SendUntranslatedIfUnavailable = true;

        _output.SendMessage("Test3", flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_translator.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(_handlerB.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_handlerC.ReceivedMessages, Has.Count.EqualTo(2));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handlerC.ReceivedMessages[1], Is.EqualTo("Test3"));
        }
    }

    [Test]
    public void PreprocessorHandlingTest()
    {
        _preprocessorEarlyFull.Enabled = true;
        _preprocessorLatePartial.Enabled = true;

        _infoA.Enabled = true;
        _output.RefreshHandlers();

        var flags = OutputSettingsFlags.AllowAllOutputs | OutputSettingsFlags.DoPreprocessAll;

        _output.SendMessage("Msg1", flags);
        _output.SendNotification("Notif1", OutputNotificationPriority.Critical, flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput, Has.Count.EqualTo(2));
            Assert.That(_preprocessorLatePartial.ReceivedInput, Has.Count.EqualTo(2));
            
            Assert.That(_handlerA.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_handlerA.ReceivedNotifications, Has.Count.EqualTo(1));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput[0], Is.EqualTo("Msg1"));
            Assert.That(_preprocessorEarlyFull.ReceivedInput[1], Is.EqualTo("Notif1"));

            Assert.That(_preprocessorLatePartial.ReceivedInput[0], Is.EqualTo("Msg1"));
            Assert.That(_preprocessorLatePartial.ReceivedInput[1], Is.EqualTo("Notif1"));

            Assert.That(_handlerA.ReceivedMessages[0], Is.EqualTo("Msg1"));
            Assert.That(_handlerA.ReceivedNotifications[0].Message, Is.EqualTo("Notif1"));
        }

        _preprocessorEarlyFull.ContinueIfHandled = false;

        _output.SendMessage("Msg2", flags);
        _output.SendNotification("Notif2", OutputNotificationPriority.Critical, flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput, Has.Count.EqualTo(4));
            Assert.That(_preprocessorLatePartial.ReceivedInput, Has.Count.EqualTo(4));
            
            Assert.That(_handlerA.ReceivedMessages, Has.Count.EqualTo(2));
            Assert.That(_handlerA.ReceivedNotifications, Has.Count.EqualTo(2));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput[2], Is.EqualTo("Msg2"));
            Assert.That(_preprocessorEarlyFull.ReceivedInput[3], Is.EqualTo("Notif2"));

            Assert.That(_preprocessorLatePartial.ReceivedInput[2], Is.EqualTo("Msg2"));
            Assert.That(_preprocessorLatePartial.ReceivedInput[3], Is.EqualTo("Notif2"));

            Assert.That(_handlerA.ReceivedMessages[1], Is.EqualTo("Msg2"));
            Assert.That(_handlerA.ReceivedNotifications[1].Message, Is.EqualTo("Notif2"));
        }

        _preprocessorEarlyFull.ProcessedOutput = "Processed";

        _output.SendMessage("Msg3", flags);
        _output.SendNotification("Notif3", OutputNotificationPriority.Critical, flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput, Has.Count.EqualTo(6));
            Assert.That(_preprocessorLatePartial.ReceivedInput, Has.Count.EqualTo(4));
            
            Assert.That(_handlerA.ReceivedMessages, Has.Count.EqualTo(2));
            Assert.That(_handlerA.ReceivedNotifications, Has.Count.EqualTo(2));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput[4], Is.EqualTo("Msg3"));
            Assert.That(_preprocessorEarlyFull.ReceivedInput[5], Is.EqualTo("Notif3"));
        }

        _preprocessorEarlyFull.ContinueIfHandled = true;

        _output.SendMessage("Msg4", flags);
        _output.SendNotification("Notif4", OutputNotificationPriority.Critical, flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput, Has.Count.EqualTo(8));
            Assert.That(_preprocessorLatePartial.ReceivedInput, Has.Count.EqualTo(6));
            
            Assert.That(_handlerA.ReceivedMessages, Has.Count.EqualTo(3));
            Assert.That(_handlerA.ReceivedNotifications, Has.Count.EqualTo(3));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput[6], Is.EqualTo("Msg4"));
            Assert.That(_preprocessorEarlyFull.ReceivedInput[7], Is.EqualTo("Notif4"));

            Assert.That(_preprocessorLatePartial.ReceivedInput[4], Is.EqualTo("Processed"));
            Assert.That(_preprocessorLatePartial.ReceivedInput[5], Is.EqualTo("Processed"));

            Assert.That(_handlerA.ReceivedMessages[2], Is.EqualTo("Processed"));
            Assert.That(_handlerA.ReceivedNotifications[2].Message, Is.EqualTo("Processed"));
        }

        _preprocessorLatePartial.ProcessedOutput = "Processed2";

        _output.SendMessage("Msg5", flags);
        _output.SendNotification("Notif5", OutputNotificationPriority.Critical, flags);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput, Has.Count.EqualTo(10));
            Assert.That(_preprocessorLatePartial.ReceivedInput, Has.Count.EqualTo(8));
            
            Assert.That(_handlerA.ReceivedMessages, Has.Count.EqualTo(4));
            Assert.That(_handlerA.ReceivedNotifications, Has.Count.EqualTo(4));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput[8], Is.EqualTo("Msg5"));
            Assert.That(_preprocessorEarlyFull.ReceivedInput[9], Is.EqualTo("Notif5"));

            Assert.That(_preprocessorLatePartial.ReceivedInput[6], Is.EqualTo("Processed"));
            Assert.That(_preprocessorLatePartial.ReceivedInput[7], Is.EqualTo("Processed"));

            Assert.That(_handlerA.ReceivedMessages[3], Is.EqualTo("Processed2"));
            Assert.That(_handlerA.ReceivedNotifications[3].Message, Is.EqualTo("Processed2"));
        }
    }

    [Test]
    public void PreprocessorEnabledTest()
    {
        _preprocessorEarlyFull.Enabled = true;
        _preprocessorLatePartial.Enabled = true;

        var flags = OutputSettingsFlags.AllowAllOutputs | OutputSettingsFlags.DoPreprocessAll;
        
        _output.SendMessage("Test", flags);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(_preprocessorLatePartial.ReceivedInput, Has.Count.EqualTo(1));
        }

        _preprocessorLatePartial.Enabled = false;

        _output.SendMessage("Test2", flags);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_preprocessorEarlyFull.ReceivedInput, Has.Count.EqualTo(2));
            Assert.That(_preprocessorLatePartial.ReceivedInput, Has.Count.EqualTo(1));
        }
    }
    
    protected override void OneTimeTearDownExtra()
    {
        _output.Stop();
    }
}