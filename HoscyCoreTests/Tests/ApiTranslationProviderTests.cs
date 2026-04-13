using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Translation.Core;
using HoscyCore.Services.Translation.Modules;
using HoscyCoreTests.Mocks.Impl;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.ApiTranslationProviderTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class ApiTranslationProviderStartupTests : TestBase<ApiTranslationProviderStartupTests>
{
    private ConfigModel _config = null!;
    private MockApiClient _client = null!;

    private ApiTranslationModule _provider = null!;

    protected override void SetupExtra()
    {
        _config = new();
        _config.Api_Presets.Clear();

        _client = new();

        _provider = new(_logger, _config, _client);
    }

    [Test]
    public void InvalidModelTest()
    {
        _config.Translation_Api_Preset = "Test";
        _provider.Start().AssertFail();
    }

    [Test]
    public void ModelNotLoadedTest()
    {
        _config.Translation_Api_Preset = "Test";
        _config.Api_Presets.Add(new() { Name = "Test" });

        _client.PresetLoadSuccessful = false;
        _provider.Start().AssertFail();
    }

    [TestCase(false, false), TestCase(true, false), TestCase(false, true)]
    public void StartStopRestartTest(bool restartNotStart, bool doAgain)
    {
        _config.Translation_Api_Preset = "Test";
        var preset = new ApiPresetModel() { Name = "Test" };
        _config.Api_Presets.Add(preset);

        _client.PresetLoadSuccessful = true;

        _provider.Start().AssertOk();
        using (Assert.EnterMultipleScope()) {
            AssertServiceProcessing(_provider);
            Assert.That(_client.LoadedModel, Is.EqualTo(preset));
        }

        if (restartNotStart)
            _provider.Restart().AssertOk();
        else 
            _provider.Start().AssertOk();
        using (Assert.EnterMultipleScope()) {
            AssertServiceProcessing(_provider);
            Assert.That(_client.LoadedModel, Is.EqualTo(preset));
        }

        _provider.Stop().AssertOk();
        using (Assert.EnterMultipleScope()) {
            AssertServiceStopped(_provider);
            Assert.That(_client.LoadedModel, Is.Null);
        }

        if (!doAgain) return;

        _provider.Start().AssertOk();
        using (Assert.EnterMultipleScope()) {
            AssertServiceProcessing(_provider);
            Assert.That(_client.LoadedModel, Is.EqualTo(preset));
        }

        _provider.Stop().AssertOk();
        using (Assert.EnterMultipleScope()) {
            AssertServiceStopped(_provider);
            Assert.That(_client.LoadedModel, Is.Null);
        }
    }
}

public class ApiTranslationProviderFunctionTests : TestBase<ApiTranslationProviderFunctionTests>
{
    private readonly ConfigModel _config = new();
    private readonly MockApiClient _client = new();

    private ApiTranslationModule _provider = null!;

    protected override void OneTimeSetupExtra()
    {
        _config.Api_Presets.Clear();
        _config.Api_Presets.Add(new() { Name = "Test" });
        _config.Translation_Api_Preset = "Test";

        var provider = new ApiTranslationModule(_logger, _config, _client);
        provider.Start().AssertOk();
        _provider = provider;
    }

    protected override void SetupExtra()
    {
        _client.ClearReceived();
        _provider.ClearFault();
        _client.ErrorOnSend = false;
    }

    [Test]
    public void EmptyTest()
    {
        var result = _provider.TryTranslate(string.Empty, out var output);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TranslationResult.Failed));
            Assert.That(_client.ReceivedStrings, Is.Empty);
            Assert.That(output, Is.Null);
            AssertServiceProcessing(_provider);
        }
    }

    [Test]
    public void EmptyTranslationTest()
    {
        _client.SendTextResult = string.Empty;

        var result = _provider.TryTranslate("Test", out var output);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TranslationResult.Failed));
            Assert.That(_client.ReceivedStrings, Is.Not.Empty);
            Assert.That(output, Is.Null);
            AssertServiceProcessing(_provider);
        }
        Assert.That(_client.ReceivedStrings[0], Is.EqualTo("Test"));
    }

    [Test]
    public void ExceptionTest()
    {
        _client.SendTextResult = "Yay";
        _client.ErrorOnSend = true;

        var result = _provider.TryTranslate("Test", out var output);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TranslationResult.Failed));
            Assert.That(_client.ReceivedStrings, Is.Not.Empty);
            Assert.That(output, Is.Null);
            AssertServiceFaulted(_provider);
        }
        Assert.That(_client.ReceivedStrings[0], Is.EqualTo("Test"));
    }

    [Test]
    public void SuccessTest()
    {
        _client.SendTextResult = "Yay";

        var result = _provider.TryTranslate("Test", out var output);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(TranslationResult.Succeeded));
            Assert.That(_client.ReceivedStrings, Is.Not.Empty);
            Assert.That(output, Is.EqualTo("Yay"));
            AssertServiceProcessing(_provider);
        }
        Assert.That(_client.ReceivedStrings[0], Is.EqualTo("Test"));
    }

    protected override void OneTimeTearDownExtra()
    {
        _provider.Stop().AssertOk();
    }
}