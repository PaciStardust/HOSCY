using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Network;

public interface IApiClient : IService
{
    public IApiClient AddIdentifier(string identifier);
    public void ClearPreset();
    public bool IsPresetLoaded();
    public bool LoadPreset(ApiPresetModel preset);
    public Task<string> SendBytes(byte[] bytes);
    public Task<string> SendText(string text);
}