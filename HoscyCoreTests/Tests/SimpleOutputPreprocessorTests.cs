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

    private FileCommandOutputPreprocessor _filePre = null!;
    private OscCommandOutputPreprocessor _oscPre = null!;

    protected override void OneTimeSetupExtra()
    {
        _filePre = new(_logger);
        _oscPre = new(_oscCommand, _logger);
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
}