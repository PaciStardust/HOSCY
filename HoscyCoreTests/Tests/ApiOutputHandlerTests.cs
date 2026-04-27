using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Output.Handlers;
using HoscyCoreTests.Mocks.Impl;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.ApiOutputHandlerTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class ApiOutputHandlerTests : TestBase<ApiOutputHandlerTests>
{
    private readonly MockApiClient _api = new();
    private readonly ConfigModel _config = new();
    private ApiOutputHandler _handler = null!;

    protected override void OneTimeSetupExtra()
    {
        _handler = new(_logger, _api, _config);
    }

    protected override void SetupExtra()
    {
        _handler.ClearFault();

        _config.Api_Presets.Clear();
        _config.ApiOut_Preset_Clear = string.Empty;
        _config.ApiOut_Preset_Message = string.Empty;
        _config.ApiOut_Preset_Notification = string.Empty;
        _config.ApiOut_Preset_Processing = string.Empty;

        _api.ClearReceived();
        _api.ClearPreset();
        _api.ErrorOnSend = false;
        _api.PresetLoadSuccessful = true;
    }

    private void SendTest(Action<ApiOutputHandler> action, Action<string> setConf)
    {
        AssertServiceProcessing(_handler);

        _handler.Clear();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_handler);
            Assert.That(_api.LoadedModel, Is.Null);
            Assert.That(_api.ReceivedStrings, Is.Empty);
        }

        setConf("Preset");

        action(_handler);
        using (Assert.EnterMultipleScope())
        {
            AssertServiceFaulted(_handler);
            Assert.That(_api.LoadedModel, Is.Null);
            Assert.That(_api.ReceivedStrings, Is.Empty);
        }

        _handler.ClearFault();

        var preset = new ApiPresetModel()
        {
            Name = "Preset"
        };
        _config.Api_Presets.Add(preset);
        _api.PresetLoadSuccessful = false;

        action(_handler);
        using (Assert.EnterMultipleScope())
        {
            AssertServiceFaulted(_handler);
            Assert.That(_api.LoadedModel, Is.Null);
            Assert.That(_api.ReceivedStrings, Is.Empty);
        }

        _handler.ClearFault();

        _api.ErrorOnSend = true;
        _api.PresetLoadSuccessful = true;

        action(_handler);
        Thread.Sleep(50);
        using (Assert.EnterMultipleScope())
        {
            AssertServiceFaulted(_handler);
            Assert.That(_api.LoadedModel, Is.Not.Null);
            Assert.That(_api.ReceivedStrings, Is.Not.Empty);
        }

        _api.ClearPreset();
        _api.ClearReceived();
        _handler.ClearFault();

        _api.ErrorOnSend = false;

        action(_handler);
        Thread.Sleep(50);
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_handler);
            Assert.That(_api.LoadedModel, Is.Not.Null);
            Assert.That(_api.ReceivedStrings, Is.Not.Empty);
        }
    }

    [Test]
    public void ClearTest()
    {
        SendTest(x => x.Clear(), x => _config.ApiOut_Preset_Clear = x);
    }

    [Test]
    public void MessageTest()
    {
        SendTest(x => x.HandleMessage("Hii"), x => _config.ApiOut_Preset_Message = x);
    }

    [Test]
    public void NotificationTest()
    {
        SendTest(x => x.HandleNotification("Waaa", OutputNotificationPriority.Critical), x => _config.ApiOut_Preset_Notification = x);
    }

    [Test]
    public void ProcessingTest()
    {
        SendTest(x => x.SetProcessingIndicator(true), x => _config.ApiOut_Preset_Processing = x);
    }

    [Test]
    public void TrueFalseTest()
    {
        _config.ApiOut_Value_True = "Trueeeeee";
        _config.ApiOut_Value_False = "Faaaaalse";

        _config.ApiOut_Preset_Processing = "Preset";
        _config.Api_Presets.Add(new() { Name = "Preset" });

        _handler.SetProcessingIndicator(true);
        Assert.That(_api.ReceivedStrings, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_api.ReceivedStrings[0], Does.Contain(_config.ApiOut_Value_True));
            Assert.That(_api.ReceivedStrings[0], Does.Not.Contain(_config.ApiOut_Value_False));
        }

        _handler.SetProcessingIndicator(false);
        Assert.That(_api.ReceivedStrings, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_api.ReceivedStrings[1], Does.Not.Contain(_config.ApiOut_Value_True));
            Assert.That(_api.ReceivedStrings[1], Does.Contain(_config.ApiOut_Value_False));
        }
    }

    [Test]
    public void PrependPriorityTest()
    {
        _config.ApiOut_PrependNotificationPriority = false;

        _config.ApiOut_Preset_Notification = "Preset";
        _config.Api_Presets.Add(new() { Name = "Preset" });

        var baseMsg = "ThisIs A Test";

        _handler.HandleNotification(baseMsg, OutputNotificationPriority.Medium);
        Assert.That(_api.ReceivedStrings, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_api.ReceivedStrings[0], Does.Contain(baseMsg));
            Assert.That(_api.ReceivedStrings[0], Does.Not.Contain(OutputNotificationPriority.Medium.ToString()));
        }

        _config.ApiOut_PrependNotificationPriority = true;

        _handler.HandleNotification(baseMsg, OutputNotificationPriority.Medium);
        Assert.That(_api.ReceivedStrings, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_api.ReceivedStrings[1], Does.Contain(baseMsg));
            Assert.That(_api.ReceivedStrings[1], Does.Contain(OutputNotificationPriority.Medium.ToString()));
        }
    }
}