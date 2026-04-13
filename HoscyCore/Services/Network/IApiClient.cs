using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Network;

public interface IApiClient : IService
{
    public IApiClient AddIdentifier(string identifier);
    public void ClearPreset();
    public bool IsPresetLoaded();
    public bool IsPresetValid();
    public Res LoadPreset(ApiPresetModel preset);
    public Task<Res<string>> SendBytesAsync(byte[] bytes);
    public Task<Res<string>> SendTextAsync(string text);
}