using HoscyCore.Services.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Voice.Core;

public abstract class VoiceModuleBase(ILogger logger) : StartStopModuleBase(logger), IVoiceModule
{
    public abstract Task<Res> CreateAudio(string message, Stream stream, CancellationToken ct);
}