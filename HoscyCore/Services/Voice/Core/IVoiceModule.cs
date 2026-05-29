using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Voice.Core; //todo: mystery translation error?

public interface IVoiceModuleStartInfo : ISoloModuleStartInfo
{
    public VoiceModuleConfigFlags ConfigFlags { get; }
}

[Flags]
public enum VoiceModuleConfigFlags
{
    None = 0,
    PiperWeb = 1
}

public interface IVoiceModule : IStartStopModule
{
    public Task<Res> CreateAudio(string message, Stream stream, CancellationToken ct);
}