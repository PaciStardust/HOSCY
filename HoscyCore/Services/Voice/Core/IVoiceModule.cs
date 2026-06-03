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
    None = 0b0,
    PiperWeb = 0b1,
    Azure = 0b10,
    Windows = 0b100
}

public interface IVoiceModule : IStartStopModule
{
    public Task<Res> CreateAudio(string message, Stream stream, CancellationToken ct);
}