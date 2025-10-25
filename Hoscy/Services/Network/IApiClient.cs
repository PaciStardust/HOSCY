using Hoscy.Configuration.Modern;

namespace Hoscy.Services.Network;

public interface IApiClient
{
    public ApiClient AddIdentifier(string identifier);
    public void ClearPreset();
    public bool IsPresetLoaded();
    public bool LoadPreset(ApiPresetModel preset);
}