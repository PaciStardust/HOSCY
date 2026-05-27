using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Voice.Core;

public interface IVoiceModuleStartInfo : ISoloModuleStartInfo
{
    public VoiceModuleConfigFlags ConfigFlags { get; }
}

[Flags]
public enum VoiceModuleConfigFlags
{
    None = 0
}

public interface IVoiceModule : IStartStopModule
{
    public Task<Res> CreateAudio(Stream stream);
}