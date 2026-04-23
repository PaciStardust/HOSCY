using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Output.Preprocessing;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.ReplacementOutputPreprocessorTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class ReplacementOutputPreprocessorFunctionTests : TestBase<ReplacementOutputPreprocessorFunctionTests>
{
    private readonly ConfigModel _config = new();

    private FullReplacementOutputPreprocessor _fullPre = null!;
    private PartialReplacementOutputPreprocessor _partPre = null!;

    protected override void OneTimeSetupExtra()
    {
        _config.Preprocessing_ReplacementsFull.Clear();
        _config.Preprocessing_ReplacementsPartial.Clear();

        _fullPre = new(_config, _logger);
        _partPre = new(_config, _logger);
    }

    protected override void SetupExtra()
    {
        _config.Preprocessing_ReplacementsFull.Clear();
        _config.Preprocessing_ReplacementsPartial.Clear();

        _partPre.ReloadReplacements().AssertOk();
        _fullPre.ReloadReplacements().AssertOk();
    }

    [Test]
    public void IsFullEnabledTest()
    {
        _config.Preprocessing_DoReplacementsFull = false;
        Assert.That(_fullPre.IsEnabled(), Is.False);

        _config.Preprocessing_DoReplacementsFull = true;
        Assert.That(_fullPre.IsEnabled(), Is.True);
    }

    [Test]
    public void IsPartialEnabledTest()
    {
        _config.Preprocessing_DoReplacementsPartial = false;
        Assert.That(_partPre.IsEnabled(), Is.False);

        _config.Preprocessing_DoReplacementsPartial = true;
        Assert.That(_partPre.IsEnabled(), Is.True);
    }

    [Test]
    public void FullLoadingTest()
    {
        var modelA = new ReplacementDataModel() { Enabled = true, Text = "hello", Replacement = "goodbye" };
        var modelB = new ReplacementDataModel() { Enabled = true, Text = "hello2(", Replacement = "goodbye2", UseRegex = true };
        var modelC = new ReplacementDataModel() { Enabled = false, Text = "hello3", Replacement = "goodbye3" };

        _config.Preprocessing_ReplacementsFull.AddRange([
            modelA,
            modelB,
            modelC
        ]);

        _fullPre.ReloadReplacements().AssertFail();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_fullPre.LastLoadBroken, Is.EqualTo(1));
            Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_fullPre.LastLoadDisabled, Is.EqualTo(1));
            Assert.That(_fullPre.LastLoadBroken + _fullPre.LastLoadCorrect + _fullPre.LastLoadDisabled, Is.EqualTo(3));
        }
    }

    [Test]
    public void PartialLoadingTest()
    {
        var modelA = new ReplacementDataModel() { Enabled = true, Text = "hello", Replacement = "goodbye" };
        var modelB = new ReplacementDataModel() { Enabled = true, Text = "hello2(", Replacement = "goodbye2", UseRegex = true };
        var modelC = new ReplacementDataModel() { Enabled = false, Text = "hello3", Replacement = "goodbye3" };

        _config.Preprocessing_ReplacementsPartial.AddRange([
            modelA,
            modelB,
            modelC
        ]);

        _partPre.ReloadReplacements().AssertFail();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_partPre.LastLoadBroken, Is.EqualTo(1));
            Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_partPre.LastLoadDisabled, Is.EqualTo(1));
            Assert.That(_partPre.LastLoadBroken + _partPre.LastLoadCorrect + _partPre.LastLoadDisabled, Is.EqualTo(3));
        }
    }

    [Test]
    public void FullBasicTest()
    {
        var model = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "goodbye",
            Text = "hello",
            UseRegex = false
        };
        _config.Preprocessing_ReplacementsFull.Add(model);

        string[] messages = ["HELLO world", "hello world", "HELLO", "hello"];
        _fullPre.ReloadReplacements().AssertOk();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_fullPre.Process(ref messages[0]), Is.EqualTo(OutputPreprocessorResult.NotProcessed));
            Assert.That(_fullPre.Process(ref messages[1]), Is.EqualTo(OutputPreprocessorResult.NotProcessed));
            Assert.That(_fullPre.Process(ref messages[2]), Is.EqualTo(OutputPreprocessorResult.NotProcessed));
        }

        var result = _fullPre.Process(ref messages[3]);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));
            Assert.That(messages[3], Is.EqualTo("goodbye"));
        }
    }

    [Test]
    public void PartialBasicTest()
    {
        var model = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "goodbye",
            Text = "hello",
            UseRegex = false
        };
        _config.Preprocessing_ReplacementsPartial.Add(model);

        string[] messages = ["HELLO world", "HELLO", "hello", "hello world"];
        _partPre.ReloadReplacements().AssertOk();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_partPre.Process(ref messages[0]), Is.EqualTo(OutputPreprocessorResult.NotProcessed));
            Assert.That(_partPre.Process(ref messages[1]), Is.EqualTo(OutputPreprocessorResult.NotProcessed));
        }

        var result = _partPre.Process(ref messages[2]);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));
            Assert.That(messages[2], Is.EqualTo("goodbye"));
        }

        result = _partPre.Process(ref messages[3]);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));
            Assert.That(messages[3], Is.EqualTo("goodbye world"));
        }
    }

    [Test]
    public void IgnoreCaseTest()
    {
        var model = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "goodbye",
            Text = "hello",
            UseRegex = false
        };
        _config.Preprocessing_ReplacementsPartial.Add(model);
        _config.Preprocessing_ReplacementsFull.Add(model);

        _partPre.ReloadReplacements().AssertOk();
        _fullPre.ReloadReplacements().AssertOk();

        string[] messages = ["HELLO world", "HELLO", "hello world", "hello"]; 
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(1));

            Assert.That(_partPre.Process(ref messages[0]), Is.EqualTo(OutputPreprocessorResult.NotProcessed));
            Assert.That(_fullPre.Process(ref messages[1]), Is.EqualTo(OutputPreprocessorResult.NotProcessed));
        }

        var resPart = _partPre.Process(ref messages[2]);
        var resFull = _fullPre.Process(ref messages[3]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resPart, Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));
            Assert.That(resFull, Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));

            Assert.That(messages[2], Is.EqualTo("goodbye world"));
            Assert.That(messages[3], Is.EqualTo("goodbye"));
        }

        model.IgnoreCase = true;
        messages = ["HELLO world", "HELLO", "hello world", "hello"]; 

        _partPre.ReloadReplacements().AssertOk();
        _fullPre.ReloadReplacements().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(1));
        }

        resPart = _partPre.Process(ref messages[2]);
        resFull = _fullPre.Process(ref messages[3]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resPart, Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));
            Assert.That(resFull, Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));

            Assert.That(messages[2], Is.EqualTo("goodbye world"));
            Assert.That(messages[3], Is.EqualTo("goodbye"));
        }

        resPart = _partPre.Process(ref messages[0]);
        resFull = _fullPre.Process(ref messages[1]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resPart, Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));
            Assert.That(resFull, Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));

            Assert.That(messages[0], Is.EqualTo("goodbye world"));
            Assert.That(messages[1], Is.EqualTo("goodbye"));
        }
    }

    [Test]
    public void RegexTest()
    {
        List<(string Input, OutputPreprocessorResult StateFull, OutputPreprocessorResult StatePartial, string Output)> testValues = [
            ("hello world",     OutputPreprocessorResult.NotProcessed,              OutputPreprocessorResult.ProcessedContinue,     "goodbye world"),
            ("hello",           OutputPreprocessorResult.ProcessedContinue,         OutputPreprocessorResult.ProcessedContinue,     "goodbye"),
            ("Hello world",     OutputPreprocessorResult.NotProcessed,              OutputPreprocessorResult.ProcessedContinue,     "goodbye world"),
            ("Hello",           OutputPreprocessorResult.ProcessedContinue,         OutputPreprocessorResult.ProcessedContinue,     "goodbye"),
            ("Hellooooo world", OutputPreprocessorResult.NotProcessed,              OutputPreprocessorResult.ProcessedContinue,     "goodbye world"),
            ("Hellooooo",       OutputPreprocessorResult.ProcessedContinue,         OutputPreprocessorResult.ProcessedContinue,     "goodbye")
        ];

        var model = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "goodbye",
            Text = "[hH]ello+",
            UseRegex = false
        };
        _config.Preprocessing_ReplacementsPartial.Add(model);
        _config.Preprocessing_ReplacementsFull.Add(model);

        _partPre.ReloadReplacements().AssertOk();
        _fullPre.ReloadReplacements().AssertOk();

        string[] rgxMsg = ["[hH]ello+ world", "[hH]ello+"];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(1));

            Assert.That(_partPre.Process(ref rgxMsg[0]), Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));
            Assert.That(_fullPre.Process(ref rgxMsg[1]), Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));

            foreach(var (input, _, _, _) in testValues)
            {
                var inputCpy = input;
                Assert.That(_partPre.Process(ref inputCpy), Is.EqualTo(OutputPreprocessorResult.NotProcessed), input);
                Assert.That(_fullPre.Process(ref inputCpy), Is.EqualTo(OutputPreprocessorResult.NotProcessed), input);
                Assert.That(input, Is.EqualTo(inputCpy));
            }
        }

        model.UseRegex = true;

        _partPre.ReloadReplacements().AssertOk();
        _fullPre.ReloadReplacements().AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(1));
        }

        using (Assert.EnterMultipleScope())
        {
            foreach (var (input, passFull, passPart, expectedOutput) in testValues)
            {
                var inputCpyPart = input;
                var inputCpyFull = input;

                var resPart = _partPre.Process(ref inputCpyPart);
                Assert.That(resPart, Is.EqualTo(passPart), input);
                if (passPart == OutputPreprocessorResult.ProcessedContinue)
                    Assert.That(inputCpyPart, Is.EqualTo(expectedOutput), input);

                var resFull = _fullPre.Process(ref inputCpyFull);
                Assert.That(resFull, Is.EqualTo(passFull), input);
                if (passFull == OutputPreprocessorResult.ProcessedContinue)
                    Assert.That(inputCpyFull, Is.EqualTo(expectedOutput), input);
            }
        }
    }

    [Test]
    public void PartialMultiTest()
    {
        var modelA = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "goodbye",
            Text = "hello",
            UseRegex = false
        };
        var modelB = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "?",
            Text = "!",
            UseRegex = false
        };
        var modelC = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "space",
            Text = "world",
            UseRegex = false
        };
        var modelD = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "cat",
            Text = "space",
            UseRegex = false
        };

        _config.Preprocessing_ReplacementsPartial.AddRange([
            modelA,
            modelB,
            modelC,
            modelD
        ]);

        _partPre.ReloadReplacements().AssertOk();
        Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(4));

        var txt = "hello world";
        var res = _partPre.Process(ref txt);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(res, Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));
            Assert.That(txt, Is.EqualTo("goodbye cat"));
        }
    }

    [Test]
    public void FullMultiTest()
    {
        var modelA = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "goodbye world",
            Text = "hello world",
            UseRegex = false
        };
        var modelB = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "goodbye world",
            Text = "hello cat",
            UseRegex = false
        };

        _config.Preprocessing_ReplacementsFull.AddRange([
            modelA,
            modelB
        ]);

        _fullPre.ReloadReplacements().AssertOk();
        Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(2));

        var txt = "hello world";
        var res = _fullPre.Process(ref txt);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(res, Is.EqualTo(OutputPreprocessorResult.ProcessedContinue));
            Assert.That(txt, Is.EqualTo("goodbye world"));
        }
    }
}