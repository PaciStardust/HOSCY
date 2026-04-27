using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Impl;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.RecognitionManagerServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public abstract class RecognitionManagerServiceTestBase<T> : SoloModuleManagerTestBase
<
    T,
    IRecognitionModuleStartInfo,
    MockRecognitionModuleStartInfo,
    IRecognitionModule,
    MockRecognitionModuleA,
    MockRecognitionModuleB,
    RecognitionManagerService
> 
{
    protected ConfigModel _config = null!;
    protected MockOutputManagerService _output = null!;

    protected override RecognitionManagerService CreateController()
    {
        return new RecognitionManagerService(_notify, _logger, _infoLoader, _moduleLoader, _config, _output);
    }

    protected override void SetupSharedClassesExtra()
    {
        _config = new();
        _output = new();
    }

    protected override void SetModule(string name)
    {
        _config.Recognition_SelectedModuleName = name;
    }
}

public class RecognitionManagerServiceFunctionTests : RecognitionManagerServiceTestBase<RecognitionManagerServiceFunctionTests>
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

        _config.Recognition_Mute_StartUnmuted = false;
        _config.Recognition_Fixup_RemoveEndPeriod = false;
        _config.Recognition_Fixup_CapitalizeFirstLetter = false;
        _config.Recognition_Fixup_NoiseFilter.Clear();
        _manager.UpdateSettings();

        _notify.Notifications.Clear();
        _output.Clear();

        _moduleA.ResetStats();
        _moduleB.ResetStats();
    }

    [TestCase(false), TestCase(true)]
    public void ModuleStartStopTest(bool autoMute)
    {
        _config.Recognition_Mute_StartUnmuted = autoMute;

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();
        
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_manager);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Not.Null);
            Assert.That(_manager.IsListening, Is.EqualTo(autoMute));
        }

        _manager.StopModule().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            AssertServiceStarted(_manager);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_manager.IsListening, Is.False);
        }
    }

    [Test]
    public void ModuleStartWithListenErrorTest()
    {
        _config.Recognition_Mute_StartUnmuted = true;
        _moduleA.FailOnSetListening = true;

        SetModule(_infoA.Name);
        _manager.StartModule().AssertFail();
        
        using (Assert.EnterMultipleScope())
        {
            AssertServiceFaulted(_manager);
            Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
            Assert.That(_manager.IsListening, Is.EqualTo(false));
        }
    }

    [Test]
    public void ModuleStateChangeTest()
    {
        static void AssertArgs(List<RecognitionStatusChangedEventArgs> list, int count, bool listening, bool stopped)
        {
            Assert.That(list, Has.Count.EqualTo(count));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(list[count - 1].IsListening, Is.EqualTo(listening));
                Assert.That(list[count - 1].Status, stopped ? Is.EqualTo(ServiceStatus.Stopped) : Is.Not.EqualTo(ServiceStatus.Stopped));
            }
        }

        _config.Recognition_Mute_StartUnmuted = false;

        List<RecognitionStatusChangedEventArgs> receivedArgs = [];
        _manager.OnModuleStatusChanged += (_, e) => receivedArgs.Add(e);

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();
        AssertArgs(receivedArgs, 1, false, false);

        _manager.SetListening(true).AssertOk();;
        AssertArgs(receivedArgs, 2, true, false);

        _manager.SetListening(false).AssertOk();;
        AssertArgs(receivedArgs, 3, false, false);

        _manager.StopModule().AssertOk();
        AssertArgs(receivedArgs, 4, false, true);

        _config.Recognition_Mute_StartUnmuted = true;
        _manager.StartModule().AssertOk();
        AssertArgs(receivedArgs, 5, true, false);

        _manager.StopModule().AssertOk();
        AssertArgs(receivedArgs, 6, false, true);

        _moduleA.ResultToReturn = ResC.Fail("aaa");
        _manager.StartModule().AssertFail();
        AssertArgs(receivedArgs, 6, false, true);

        _moduleA.ResultToReturn = null;
        _manager.StartModule().AssertOk();
        AssertArgs(receivedArgs, 7, true, false);

        _moduleA.Stop();
        AssertArgs(receivedArgs, 8, false, true);

        _manager.StartModule().AssertOk();
        AssertArgs(receivedArgs, 9, true, false);

        _moduleA.ResultToReturn = ResC.Fail("aaa");
        _manager.StopModule().AssertFail();
        AssertArgs(receivedArgs, 10, false, true);
    }

    [Test]
    public void SetListeningTest()
    {
        void AssertListening(bool state)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_moduleA.IsListening, Is.EqualTo(state));
                Assert.That(_manager.IsListening, Is.EqualTo(state));
            }
        }

        _config.Recognition_Mute_StartUnmuted = false;
        SetModule(_infoA.Name);
        AssertListening(false);

        _manager.SetListening(true).AssertFail();
        AssertListening(false);

        _manager.StartModule().AssertOk();
        AssertListening(false);

        _manager.SetListening(true).AssertOk();
        AssertListening(true);

        _manager.SetListening(true).AssertOk();
        AssertListening(true);

        _manager.SetListening(false).AssertOk();
        AssertListening(false);

        _moduleA.SetListening(false);
        _moduleA.InvokeInternalListeningStatusChange();
        AssertListening(false);

        _manager.StopModule().AssertOk();

        _manager.SetListening(true).AssertFail();
        AssertListening(false);
    }

    [Test]
    public void OutputTest()
    {
        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        _moduleA.InvokeSpeechRecognized(string.Empty);
        Assert.That(_output.Messages, Is.Empty);
        
        _moduleA.InvokeSpeechRecognized("Hello");
        Assert.That(_output.Messages, Has.Count.EqualTo(1));
        Assert.That(_output.Messages[0].Message, Is.EqualTo("Hello"));
    }

    [Test]
    public void OutputFlagsTest()
    {
        var r = new Random();

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        for (var i = 0; i < 128; i++)
        {
            OutputSettingsFlags expectedFlags = OutputSettingsFlags.None;

            var res = r.Next() % 2 == 0;
            _config.Recognition_Send_ViaText = res;
            expectedFlags |= res ? OutputSettingsFlags.AllowTextOutput : OutputSettingsFlags.None;

            res = r.Next() % 2 == 0;
            _config.Recognition_Send_ViaAudio = res;
            expectedFlags |= res ? OutputSettingsFlags.AllowAudioOutput : OutputSettingsFlags.None;

            res = r.Next() % 2 == 0;
            _config.Recognition_Send_ViaOther = res;
            expectedFlags |= res ? OutputSettingsFlags.AllowOtherOutput : OutputSettingsFlags.None;

            res = r.Next() % 2 == 0;
            _config.Recognition_Send_DoTranslate = res;
            expectedFlags |= res ? OutputSettingsFlags.DoTranslate : OutputSettingsFlags.None;

            res = r.Next() % 2 == 0;
            _config.Recognition_Send_DoPreprocessPartial = res;
            expectedFlags |= res ? OutputSettingsFlags.DoPreprocessPartial : OutputSettingsFlags.None;

            res = r.Next() % 2 == 0;
            _config.Recognition_Send_DoPreprocessFull = res;
            expectedFlags |= res ? OutputSettingsFlags.DoPreprocessFull : OutputSettingsFlags.None;

            _moduleA.InvokeSpeechRecognized(i.ToString());
            Assert.That(_output.Messages, Has.Count.EqualTo(i + 1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_output.Messages[i].Message, Is.EqualTo(i.ToString()));
                Assert.That(_output.Messages[i].Flags, Is.EqualTo(expectedFlags));
            }
        }
    }

    [TestCase(true), TestCase(false)]
    public void OutputCleanupPeriodsTest(bool removePeriod)
    {
        _config.Recognition_Fixup_RemoveEndPeriod = removePeriod;

        List<(string Input, string? Output)> results = [
            ("Hello.", "Hello"),
            ("Good. Day", "Good. Day"),
            ("..Helloooo...", "..Helloooo"),
            ("..", null),
            ("Test., 。", "Test")
        ];

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        foreach(var (Input, Output) in results)
        {
            _output.Clear();

            _moduleA.InvokeSpeechRecognized(Input);

            if (!removePeriod)
            {
                Assert.That(_output.Messages, Has.Count.EqualTo(1));
                Assert.That(_output.Messages[0].Message, Is.EqualTo(Input));
                continue;
            }

            if (Output is null)
            {
                Assert.That(_output.Messages, Is.Empty);
            }
            else
            {
                Assert.That(_output.Messages, Has.Count.EqualTo(1));
                Assert.That(_output.Messages[0].Message, Is.EqualTo(Output));
            }
        }
    }

    [TestCase(true), TestCase(false)]
    public void OutputCleanupUppercaseTest(bool uppercase)
    {
        _config.Recognition_Fixup_CapitalizeFirstLetter = uppercase;

        List<(string Input, string? Output)> results = [
            ("hello", "Hello"),
            ("good. Day", "Good. Day"),
            ("..helloooo", "..helloooo"),
            ("Hii", "Hii"),
            ("test", "Test")
        ];

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        foreach(var (Input, Output) in results)
        {
            _output.Clear();

            _moduleA.InvokeSpeechRecognized(Input);

            if (!uppercase)
            {
                Assert.That(_output.Messages, Has.Count.EqualTo(1));
                Assert.That(_output.Messages[0].Message, Is.EqualTo(Input));
                continue;
            }

            Assert.That(_output.Messages, Has.Count.EqualTo(1));
            Assert.That(_output.Messages[0].Message, Is.EqualTo(Output));
        }
    }

    [TestCase(true), TestCase(false)]
    public void OutputCleanupDenoiseTest(bool setDenoise)
    {
        if (setDenoise)
        {
            _config.Recognition_Fixup_NoiseFilter.Add("noise");
            _config.Recognition_Fixup_NoiseFilter.Add("bye");
            _manager.UpdateSettings();
        }

        List<(string Input, string? Output)> results = [
            ("noise Hello", "Hello"),
            ("bye Day", "Day"),
            ("hello noise", "hello"),
            ("Hii", "Hii"),
            ("Test bye", "Test"),
            ("Test bye Test", "Test bye Test"),
            ("noise bye testing2 bye noise", "testing2"),
            ("Noise byE testing bYe noIse bye", "testing"),
            ("noise bye", null)
        ];

        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        foreach(var (Input, Output) in results)
        {
            _output.Clear();

            _moduleA.InvokeSpeechRecognized(Input);

            if (!setDenoise)
            {
                Assert.That(_output.Messages, Has.Count.EqualTo(1));
                Assert.That(_output.Messages[0].Message, Is.EqualTo(Input));
                continue;
            }

            if (Output is null)
            {
                Assert.That(_output.Messages, Is.Empty);
            }
            else
            {
                Assert.That(_output.Messages, Has.Count.EqualTo(1));
                Assert.That(_output.Messages[0].Message, Is.EqualTo(Output));
            }
        }
    }

    [Test]
    public void SpeechActivityTest()
    {
        SetModule(_infoA.Name);
        _manager.StartModule().AssertOk();

        Assert.That(_output.ProcessingIndicator, Is.False);

        _moduleA.InvokeSpeechActivity(true);
        Assert.That(_output.ProcessingIndicator, Is.True);

        _moduleA.InvokeSpeechActivity(false);
        Assert.That(_output.ProcessingIndicator, Is.False);
    }
}