using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Translation.Core;
using HoscyCore.Services.Translation.Providers;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests.ApiTranslationProviderTests;

public class ApiTranslationProviderFunctionTests : TestBaseForService<ApiTranslationProviderFunctionTests>
{
    private readonly ConfigModel _config = new();
    private readonly MockApiClient _client = new();

    private ApiTranslationProvider _provider = null!;

    protected override void OneTimeSetupExtra()
    {
        _config.Api_Presets.Clear();
        _config.Api_Presets.Add(new() { Name = "Test" });
        _config.Translation_Api_Preset = "Test";

        _provider = new(_logger, _config, _client);

        _provider.Start();
        AssertServiceProcessing(_provider);
    }

    protected override void SetupExtra()
    {
        _client.ClearReceived();
        _provider.ClearFault();
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
        _client.ThrowOnceOnSend = true;

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
        _provider.Stop();
        AssertServiceStopped(_provider);
    }
}