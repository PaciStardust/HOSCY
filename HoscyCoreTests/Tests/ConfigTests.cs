using HoscyCore.Configuration.Legacy;
using HoscyCore.Configuration.Modern;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.ConfigTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class ConfigFunctionTests : TestBase<ConfigFunctionTests>
{
    [Test]
    public void SaveAndLoad()
    {
        const string TEST_VALUE = "I am a test value";
        const string TEST_CONFIGNAME = "tstcfg.json";

        var createdConfig = new ConfigModel
        {
            Afk_StartText = TEST_VALUE
        };

        var saveRes = createdConfig.TrySave(_tempFolder, TEST_CONFIGNAME, _logger);
        Assert.That(saveRes, "Unable to save config");

        var configResult = ConfigModelLoader.TryLoad(_tempFolder, TEST_CONFIGNAME, _logger);
        Assert.That(configResult, Is.Not.Null, "Config could not be loaded");

        configResult.AssertOk();
        Assert.That(configResult.Value!.Afk_StartText, Is.EqualTo(createdConfig.Afk_StartText),
            "Loaded config does not match created config");
    }

    [Test]
    public void LoadLegacy()
    {
        var legacyConfig = LegacyConfigModelLoader.TryLoad(TestUtils.GetResourceFolder(), "old-config.json", _logger);
        Assert.That(legacyConfig, Is.Not.Null, "Failed to load legacy config");
        legacyConfig.AssertOk();
    }

    [Test]
    public void UpgradeLegacy()
    {
        var legacyConfig = new LegacyConfigModel();
        legacyConfig.Upgrade(_logger).AssertOk();
        Assert.That(legacyConfig.ConfigVersion, Is.GreaterThan(0), "Legacy config did not get upgraded");
    }

    [Test]
    public void UpgradeModern()
    {
        var config = new ConfigModel();
        config.Upgrade(_logger).AssertOk();
        Assert.That(config.ConfigVersion, Is.GreaterThan(0), "Config did not get upgraded");
    }

    [Test]
    public void MigrateLegacyToModern()
    {
        const string TEST_VALUE = "I am a test value";

        var legacyConfigResult = LegacyConfigModelLoader.TryLoad(TestUtils.GetResourceFolder(), "old-config.json", _logger);
        Assert.That(legacyConfigResult, Is.Not.Null, "Failed to load legacy config");
        legacyConfigResult.AssertOk();

        var legacyConfig = legacyConfigResult.Value;
        legacyConfig!.Osc.AfkStartText = TEST_VALUE;

        var newConfig = legacyConfig.Migrate(_logger);
        Assert.That(newConfig, Is.Not.Null, "Failed to migrate legacy config");

        Assert.That(newConfig.Afk_StartText, Is.EqualTo(TEST_VALUE), "Values did not get carried over");
    }
}