using HoscyCore.Configuration.Legacy;
using HoscyCore.Configuration.Modern;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests;

public class ConfigTests : TestBase<ConfigTests>
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

        var loadedConfig = ConfigModelLoader.TryLoad(_tempFolder, TEST_CONFIGNAME, _logger);
        Assert.That(loadedConfig, Is.Not.Null, "Config could not be loaded");

        Assert.That(loadedConfig.Afk_StartText, Is.EqualTo(createdConfig.Afk_StartText), "Loaded config does not match created config");
    }

    [Test]
    public void LoadLegacy()
    {
        var legacyConfig = LegacyConfigModelLoader.TryLoad(TestUtils.GetResourceFolder(), "old-config.json", _logger);
        Assert.That(legacyConfig, Is.Not.Null, "Failed to load legacy config");
    }

    [Test]
    public void UpgradeLegacy()
    {
        var legacyConfig = new LegacyConfigModel();
        legacyConfig.Upgrade(_logger);
        Assert.That(legacyConfig.ConfigVersion, Is.GreaterThan(0), "Legacy config did not get upgraded");
    }

    [Test]
    public void UpgradeModern()
    {
        var config = new ConfigModel();
        config.Upgrade(_logger);
        Assert.That(config.ConfigVersion, Is.GreaterThan(0), "Config did not get upgraded");
    }

    [Test]
    public void MigrateLegacyToModern()
    {
        const string TEST_VALUE = "I am a test value";

        var legacyConfig = LegacyConfigModelLoader.TryLoad(TestUtils.GetResourceFolder(), "old-config.json", _logger);
        Assert.That(legacyConfig, Is.Not.Null, "Failed to load legacy config");
        legacyConfig.Osc.AfkStartText = TEST_VALUE;

        var newConfig = legacyConfig.Migrate(_logger);
        Assert.That(newConfig, Is.Not.Null, "Failed to migrate legacy config");

        Assert.That(newConfig.Afk_StartText, Is.EqualTo(TEST_VALUE), "Values did not get carried over");
    }
}