using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Network;

namespace HoscyCoreTests.Mocks;

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
    public bool LoadPreset(ApiPresetModel preset)
    {
        LoadedModel = preset;
        return PresetLoadSuccessful;
    }

    public readonly List<byte[]> ReceivedBytes = [];
    public string SendBytesResult = string.Empty;
    public Task<string> SendBytesAsync(byte[] bytes)
    {
        ReceivedBytes.Add(bytes);
        ThrowIfNeeded();
        return Task.FromResult(SendBytesResult);
    }

    public readonly List<string> ReceivedStrings = [];
    public string SendTextResult = string.Empty;
    public Task<string> SendTextAsync(string text)
    {
        ReceivedStrings.Add(text);
        ThrowIfNeeded();
        return Task.FromResult(SendTextResult);
    }

    public void ClearReceived()
    {
        ReceivedBytes.Clear();
        ReceivedStrings.Clear();
    }

    public bool ThrowOnceOnSend { get; set; } = false; 
    private void ThrowIfNeeded()
    {
        if (!ThrowOnceOnSend) return;
        ThrowOnceOnSend = false;
        throw new Exception();
    }
}