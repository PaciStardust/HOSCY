using HoscyCore.Services.Osc.Command;
using HoscyCore.Services.Output.Preprocessing;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests;

public class SimpleOutputPreprocessorTests : TestBase<SimpleOutputPreprocessorTests>
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
        Assert.That(_oscPre.ShouldContinueIfHandled(), Is.False);
        _oscCommand.ReturnedState = OscCommandState.Success;

        List<(string Input, bool ExpectedOutput)> valueTries = [
            (_oscCommand.CommandIdentifier, true),
            ("Test123", false),
        ];

        foreach(var (input, expectedOutput) in valueTries)
        {
            var result = _oscPre.TryProcess(input, out var output);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(expectedOutput, Is.EqualTo(result));
                Assert.That(_oscCommand.PassedStrings.LastOrDefault(), result ? Is.EqualTo(input) : Is.Not.EqualTo(input));
            }
            if (result)
            {
                Assert.That(output, Does.Contain(OscCommandState.Success.ToString()));
            }
        }
    }

    [Test]
    public void FileCommandOutputPreprocessorTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_filePre.ShouldContinueIfHandled(), Is.True);
            Assert.That(_filePre.TryProcess("aaaa", out _), Is.False);
        }

        var result = _filePre.TryProcess("[file] aaa", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(output, Does.Contain("Error"));
        }

        var testfile = Path.Combine(_tempFolder, "FileTest.txt");
        File.WriteAllText(testfile, "This is a test");

        result = _filePre.TryProcess($"[file] {testfile}", out output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(output, Is.EqualTo("This is a test"));
        }
    }
}