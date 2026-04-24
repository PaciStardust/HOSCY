using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Network;
using HoscyCore.Utility;

namespace HoscyCoreTests.Mocks.Impl;

public class MockApiClient : IApiClient
{
    public string Identifier { get; private set; } = "Mock";
    public IApiClient AddIdentifier(string identifier)
    {
        Identifier = identifier;
        return this;
    }

    public void ClearPreset()
    {
        LoadedModel = null;
    }

    public bool IsPresetLoaded()
    {
        return LoadedModel is not null;
    }

    public bool PresetValid { get; set; } = true;
    public bool IsPresetValid()
    {
        return PresetValid;
    }

    public ApiPresetModel? LoadedModel { get; private set; }
    public bool PresetLoadSuccessful { get; set; } = true;
    public Res LoadPreset(ApiPresetModel preset)
    {
        if (PresetLoadSuccessful)
        {
            LoadedModel = preset;
            return ResC.Ok();
        }

        LoadedModel = null;
        return ResC.Fail(ResMsg.Err("Preset load failed"));
    }

    public readonly List<byte[]> ReceivedBytes = [];
    public string SendBytesResult = string.Empty;
    public Task<Res<string>> SendBytesAsync(byte[] bytes)
    {
        ReceivedBytes.Add(bytes);
        return Task.FromResult(ErrorOnSend ? ResC.TFail<string>(ResMsg.Err("err")) : ResC.TOk(SendBytesResult));
    }

    public readonly List<string> ReceivedStrings = [];
    public string SendTextResult = string.Empty;
    public Task<Res<string>> SendTextAsync(string text)
    {
        ReceivedStrings.Add(text);
        return Task.FromResult(ErrorOnSend ? ResC.TFail<string>(ResMsg.Err("err")) : ResC.TOk(SendTextResult));
    }

    public void ClearReceived()
    {
        ReceivedBytes.Clear();
        ReceivedStrings.Clear();
    }

    public bool ErrorOnSend { get; set; } = false; 
}