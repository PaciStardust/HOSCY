using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Voice.Core;

public interface IVoiceManagerService : ISoloModuleManager<IVoiceModuleStartInfo>
{
    public Res Enqueue(string text);
    public void Clear();
    public Res ChangePlayback(string name);
}