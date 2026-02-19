using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Translation.Core;
using HoscyCoreTests.Mocks.Impl;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.TranslatorManagerServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public abstract class TranslatorManagerServiceTestBase<T> : SoloModuleManagerTestBase
<
    T,
    ITranslationModuleStartInfo,
    MockTranslationModuleStartInfo,
    ITranslationModule,
    MockTranslationModuleA,
    MockTranslationModuleB,
    TranslationManagerService
> 
{
    protected ConfigModel _config = null!;

    protected override TranslationManagerService CreateController()
    {
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

public class TranslatorManagerServiceFunctionTests : TranslatorManagerServiceTestBase<TranslatorManagerServiceFunctionTests>
{
    protected override void OneTimeSetupExtra()
    {
        SetupSharedClasses();
        _manager.Start();
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

    [TestCase(false), TestCase(true)]
    public void TranslatorUnavailableTest(bool untranslatedIfFailed)
    {
        _config.Translation_SendUntranslatedIfFailed = untranslatedIfFailed;

        Assert.That(_manager.GetCurrentModuleInfo(), Is.Null);
        
        var result = _manager.TryTranslate("Test", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_moduleA.ReceivedInput, Is.Empty);
            Assert.That(result, Is.EqualTo(untranslatedIfFailed ? TranslationResult.UseOriginal : TranslationResult.Failed));
            Assert.That(output, Is.Null);
        }

        SetAndRefreshModuleSelection(_infoA.Name);
        _moduleA.OverrideRunningStatus = ServiceStatus.Stopped;

        result = _manager.TryTranslate("Test", out output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_moduleA.ReceivedInput, Is.Empty);
            Assert.That(result, Is.EqualTo(untranslatedIfFailed ? TranslationResult.UseOriginal : TranslationResult.Failed));
            Assert.That(output, Is.Null);
        }

        _moduleA.OverrideRunningStatus = null;
        _moduleA.ReturnedOutput = "Waa";

        result = _manager.TryTranslate("Test", out output);
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
        
        var result = _manager.TryTranslate("Test", out var output);
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

        var result = _manager.TryTranslate("0123456789", out var output);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_moduleA.ReceivedInput, Has.Count.EqualTo(1));
            Assert.That(result, Is.EqualTo(TranslationResult.Succeeded));
            Assert.That(output, Is.Not.Null);
        }

        result = _manager.TryTranslate("012345678901", out output);
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

        var result = _manager.TryTranslate(input, out var output);
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
        _manager.Stop();
    }
}