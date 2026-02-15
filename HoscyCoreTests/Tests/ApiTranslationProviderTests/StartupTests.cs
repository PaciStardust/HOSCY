using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Translation.Providers;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests.ApiTranslationProviderTests;

public class ApiTranslationProviderStartupTests : TestBase<ApiTranslationProviderStartupTests>
{
    private ConfigModel _config = null!;
    private MockApiClient _client = null!;

    private ApiTranslationProvider _provider = null!;

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
        Assert.That(_provider.Start, Throws.Exception);
    }

    [Test]
    public void ModelNotLoadedTest()
    {
        _config.Translation_Api_Preset = "Test";
        _config.Api_Presets.Add(new() { Name = "Test" });

        _client.PresetLoadSuccessful = false;

        Assert.That(_provider.Start, Throws.Exception);
    }

    [Test]
    public void StartStopRestartTest()
    {
        _config.Translation_Api_Preset = "Test";
        var preset = new ApiPresetModel() { Name = "Test" };
        _config.Api_Presets.Add(preset);

        _client.PresetLoadSuccessful = true;

        _provider.Start();
        using (Assert.EnterMultipleScope()) {
            AssertServiceProcessing(_provider);
            Assert.That(_client.LoadedModel, Is.EqualTo(preset));
        }

        _provider.Restart();
        using (Assert.EnterMultipleScope()) {
            AssertServiceProcessing(_provider);
            Assert.That(_client.LoadedModel, Is.EqualTo(preset));
        }

        _provider.Stop();
        AssertServiceStopped(_provider);
    }
}