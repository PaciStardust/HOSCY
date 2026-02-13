using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Output.Preprocessing;
using HoscyCoreTests.Utils;
using Microsoft.Extensions.Primitives;

namespace HoscyCoreTests.Tests;

public class ReplacementOutputPreprocessorTests : TestBaseForService<ReplacementOutputPreprocessorTests>
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

        _partPre.ReloadReplacements();
        _fullPre.ReloadReplacements();
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

        _fullPre.ReloadReplacements();

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

        _partPre.ReloadReplacements();

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
        Assert.That(_fullPre.ShouldContinueIfHandled(), Is.True);

        var model = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "goodbye",
            Text = "hello",
            UseRegex = false
        };
        _config.Preprocessing_ReplacementsFull.Add(model);

        _fullPre.ReloadReplacements();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_fullPre.TryProcess("HELLO world", out _), Is.False);
            Assert.That(_fullPre.TryProcess("hello world", out _), Is.False);
            Assert.That(_fullPre.TryProcess("HELLO", out _), Is.False);
        }

        var result = _fullPre.TryProcess("hello", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(output, Is.EqualTo("goodbye"));
        }
    }

    [Test]
    public void PartialBasicTest()
    {
        Assert.That(_partPre.ShouldContinueIfHandled(), Is.True);

        var model = new ReplacementDataModel()
        {
            Enabled = true,
            IgnoreCase = false,
            Replacement = "goodbye",
            Text = "hello",
            UseRegex = false
        };
        _config.Preprocessing_ReplacementsPartial.Add(model);

        _partPre.ReloadReplacements();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_partPre.TryProcess("HELLO world", out _), Is.False);
            Assert.That(_partPre.TryProcess("HELLO", out _), Is.False);
        }

        var result = _partPre.TryProcess("hello", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(output, Is.EqualTo("goodbye"));
        }

        result = _partPre.TryProcess("hello world", out output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(output, Is.EqualTo("goodbye world"));
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

        _partPre.ReloadReplacements();
        _fullPre.ReloadReplacements();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(1));

            Assert.That(_partPre.TryProcess("HELLO world", out _), Is.False);
            Assert.That(_fullPre.TryProcess("HELLO", out _), Is.False);
        }

        var resPart = _partPre.TryProcess("hello world", out var outputPart);
        var resFull = _fullPre.TryProcess("hello", out var outputFull);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resPart, Is.True);
            Assert.That(resFull, Is.True);

            Assert.That(outputPart, Is.EqualTo("goodbye world"));
            Assert.That(outputFull, Is.EqualTo("goodbye"));
        }

        model.IgnoreCase = true;

        _partPre.ReloadReplacements();
        _fullPre.ReloadReplacements();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(1));
        }

        resPart = _partPre.TryProcess("hello world", out outputPart);
        resFull = _fullPre.TryProcess("hello", out outputFull);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resPart, Is.True);
            Assert.That(resFull, Is.True);

            Assert.That(outputPart, Is.EqualTo("goodbye world"));
            Assert.That(outputFull, Is.EqualTo("goodbye"));
        }

        resPart = _partPre.TryProcess("HELLO world", out outputPart);
        resFull = _fullPre.TryProcess("HELLO", out outputFull);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resPart, Is.True);
            Assert.That(resFull, Is.True);

            Assert.That(outputPart, Is.EqualTo("goodbye world"));
            Assert.That(outputFull, Is.EqualTo("goodbye"));
        }
    }

    [Test]
    public void RegexTest()
    {
        List<(string Input, bool StateFull, bool StatePartial, string Output)> testValues = [
            ("hello world",     false,      true,   "goodbye world"),
            ("hello",           true,       true,   "goodbye"),
            ("Hello world",     false,      true,   "goodbye world"),
            ("Hello",           true,       true,   "goodbye"),
            ("Hellooooo world", false,      true,   "goodbye world"),
            ("Hellooooo",       true,       true,   "goodbye")
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

        _partPre.ReloadReplacements();
        _fullPre.ReloadReplacements();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(1));

            Assert.That(_partPre.TryProcess("[hH]ello+ world", out _), Is.True);
            Assert.That(_fullPre.TryProcess("[hH]ello+", out _), Is.True);

            foreach(var (input, _, _, _) in testValues)
            {
                Assert.That(_partPre.TryProcess(input, out _), Is.False, input);
                Assert.That(_fullPre.TryProcess(input, out _), Is.False, input);
            }
        }

        model.UseRegex = true;

        _partPre.ReloadReplacements();
        _fullPre.ReloadReplacements();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(1));
            Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(1));
        }

        using (Assert.EnterMultipleScope())
        {
            foreach (var (input, passFull, passPart, expectedOutput) in testValues)
            {
                var resPart = _partPre.TryProcess(input, out var outputPart);
                Assert.That(resPart, Is.EqualTo(passPart), input);
                if (passPart)
                    Assert.That(outputPart, Is.EqualTo(expectedOutput), input);

                var resFull = _fullPre.TryProcess(input, out var outputFull);
                Assert.That(resFull, Is.EqualTo(passFull), input);
                if (passFull)
                    Assert.That(outputFull, Is.EqualTo(expectedOutput), input);
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

        _partPre.ReloadReplacements();
        Assert.That(_partPre.LastLoadCorrect, Is.EqualTo(4));

        var res = _partPre.TryProcess("hello world", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(res, Is.True);
            Assert.That(output, Is.EqualTo("goodbye cat"));
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

        _fullPre.ReloadReplacements();
        Assert.That(_fullPre.LastLoadCorrect, Is.EqualTo(2));

        var res = _fullPre.TryProcess("hello world", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(res, Is.True);
            Assert.That(output, Is.EqualTo("goodbye world"));
        }
    }
}