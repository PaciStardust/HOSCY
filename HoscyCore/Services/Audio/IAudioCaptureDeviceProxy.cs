using HoscyCore.Configuration.Modern;
using HoscyCore.Utility;
using SoundFlow.Enums;
using SoundFlow.Extensions.WebRtc.Apm;

namespace HoscyCore.Services.Audio;

public interface IAudioCaptureDeviceProxy : IDisposable
{
    public bool IsStarted { get; }
    public bool IsListening { get; }

    public Res Start();
    public Res Stop();

    public bool SetListening(bool state);

    public void AddApmModifier(bool echoCancellation, int ecLatencyMs, bool noiseSuppression, NoiseSuppressionLevel nsLevel,
        bool gainControl, bool highPass, bool preAmp, float preAmpGain);
    public void AddApmModifier(ConfigModel config);

    public event Action<Span<byte>, Capability> OnAudioProcessed;
}