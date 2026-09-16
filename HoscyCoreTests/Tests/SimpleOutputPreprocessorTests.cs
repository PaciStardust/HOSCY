using HoscyCore.Services.Osc.Command;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Output.Preprocessing;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Impl;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.SimpleOutputPreprocessorTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class SimpleOutputPreprocessorFunctionTests : TestBase<SimpleOutputPreprocessorFunctionTests>
{
    private readonly MockOscCommandService _oscCommand = new() { CommandIdentifier = "[OSC]"};
    private readonly MockMediaControlService _mediaControl = new() { };

    private FileCommandOutputPreprocessor _filePre = null!;
    private OscCommandOutputPreprocessor _oscPre = null!;
    private MediaCommandOutputPreprocessor _mediaPre = null!;

    protected override void OneTimeSetupExtra()
    {
        _filePre = new(_logger);
        _oscPre = new(_oscCommand, _logger);
        _mediaPre = new(_mediaControl, _logger);
    }

    protected override void SetupExtra()
    {
        _mediaControl.Reset();
    }

    [Test]
    public void OscCommandOutputPreprocessorTest()
    {
        _oscCommand.ReturnedState = ResC.TOk(OscCommandState.Success);

        List<(string Input, OutputPreprocessorResult ExpectedOutput)> valueTries = [
            (_oscCommand.CommandIdentifier, OutputPreprocessorResult.ProcessedStop),
            ("Test123", OutputPreprocessorResult.NotProcessed),
        ];

        foreach(var (input, expectedOutput) in valueTries)
        {
            var inputCpy = input;

            var result = _oscPre.Process(ref inputCpy);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(expectedOutput, Is.EqualTo(result));
                Assert.That(_oscCommand.PassedStrings.LastOrDefault(), result == OutputPreprocessorResult.ProcessedStop ? Is.EqualTo(input) : Is.Not.EqualTo(input));
            }
        }
    }

    [Test]
    public void FileCommandOutputPreprocessorTest()
    {
        var txt = "aaaa";
        Assert.That(_filePre.Process(ref txt), Is.EqualTo(OutputPreprocessorResult.NotProcessed));
        Assert.That(txt, Is.EqualTo("aaaa"));

        txt = "[file] aaa";
        var result = _filePre.Process(ref txt);
        Assert.That(result, Is.EqualTo(OutputPreprocessorResult.ProcessedStopOutput));
        Assert.That(txt, Does.Contain("Error"));

        var testfile = Path.Combine(_tempFolder, "FileTest.txt");
        File.WriteAllText(testfile, "This is a test");

        txt = $"[file] {testfile}";
        result = _filePre.Process(ref txt);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(OutputPreprocessorResult.ProcessedStopOutput));
            Assert.That(txt, Is.EqualTo("This is a test"));
        }
    }

    [Test]
    public void MediaCommandOutputPreprocessorTest()
    {
        var txt = "No media command";
        Assert.That(_mediaPre.Process(ref txt), Is.EqualTo(OutputPreprocessorResult.NotProcessed));
        Assert.That(txt, Is.EqualTo("No media command"));
        AssertCallCounts(0, 0, 0, 0, 0);

        txt = "[media]";
        Assert.That(_mediaPre.Process(ref txt), Is.EqualTo(OutputPreprocessorResult.ProcessedStop));
        AssertCallCounts(0, 0, 0, 0, 0);

        string[] commandsPlay = ["resume", "START", "pLaY", "pay"];
        for (var i = 1; i <= commandsPlay.Length; i++)
        {
            txt = "[media] " + commandsPlay[i-1];
            Assert.That(_mediaPre.Process(ref txt), Is.EqualTo(OutputPreprocessorResult.ProcessedStop));
            AssertCallCounts(Math.Min(i,commandsPlay.Length - 1), 0, 0, 0, 0);
        }

        string[] commandsPause = ["pause", "STOP", "caNcel", "cannel"];
        for (var i = 1; i <= commandsPause.Length; i++)
        {
            txt = "[media] " + commandsPause[i-1];
            Assert.That(_mediaPre.Process(ref txt), Is.EqualTo(OutputPreprocessorResult.ProcessedStop));
            AssertCallCounts(commandsPlay.Length - 1, Math.Min(i,commandsPause.Length - 1), 0, 0, 0);
        }

        string[] commandsPlayPause = ["toggle", "PLAYPAUSE", "play PAUSE", "paypos"];
        for (var i = 1; i <= commandsPlayPause.Length; i++)
        {
            txt = "[media] " + commandsPlayPause[i-1];
            Assert.That(_mediaPre.Process(ref txt), Is.EqualTo(OutputPreprocessorResult.ProcessedStop));
            AssertCallCounts(commandsPlay.Length - 1, commandsPause.Length - 1, Math.Min(i,commandsPlayPause.Length - 1), 0, 0);
        }

        string[] commandsNext = ["next", "SKIP", "forwarD", "forwa"];
        for (var i = 1; i <= commandsNext.Length; i++)
        {
            txt = "[media] " + commandsNext[i-1];
            Assert.That(_mediaPre.Process(ref txt), Is.EqualTo(OutputPreprocessorResult.ProcessedStop));
            AssertCallCounts(commandsPlay.Length - 1, commandsPause.Length - 1, commandsPlayPause.Length - 1, Math.Min(i,commandsNext.Length - 1), 0);
        }

        string[] commandsPrevious = ["previous", "LAST", "prev"];
        for (var i = 1; i <= commandsPrevious.Length; i++)
        {
            txt = "[media] " + commandsPrevious[i-1];
            Assert.That(_mediaPre.Process(ref txt), Is.EqualTo(OutputPreprocessorResult.ProcessedStop));
            AssertCallCounts(commandsPlay.Length - 1, commandsPause.Length - 1, commandsPlayPause.Length - 1, commandsNext.Length - 1, Math.Min(i,commandsPrevious.Length - 1));
        }

        void AssertCallCounts(int play, int pause, int playPause, int next, int previous)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_mediaControl.CalledPlay, Is.EqualTo(play));
                Assert.That(_mediaControl.CalledPause, Is.EqualTo(pause));
                Assert.That(_mediaControl.CalledPlayPause, Is.EqualTo(playPause));
                Assert.That(_mediaControl.CalledNext, Is.EqualTo(next));
                Assert.That(_mediaControl.CalledPrevious, Is.EqualTo(previous));
            }
        }
    }
}