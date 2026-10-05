using System.Timers;
using HoscyCore.Configuration.Modern;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Enums;
using SoundFlow.Extensions.WebRtc.Apm;
using SoundFlow.Structs;

namespace HoscyCore.Services.Audio;

public class EmptyAudioCaptureDeviceProxy(ILogger logger, AudioFormat format)
    : IAudioCaptureDeviceProxy
{
    private readonly ILogger _logger = logger;
    private readonly AudioFormat _format = format;

    private System.Timers.Timer? _fakeDataTimer;
    private byte[]? _fakeDataToSend;

    public bool IsStarted { get; private set; } = false;
    public bool IsListening { get; private set; } = false;
    public event Action<Span<byte>, Capability> OnAudioProcessed = delegate { };

    public void AddApmModifier(bool echoCancellation, int ecLatencyMs, bool noiseSuppression, NoiseSuppressionLevel nsLevel, bool gainControl, bool highPass, bool preAmp, float preAmpGain)
    {
        _logger.Verbose("Received request to add ApmModifier to Empty");
    }

    public void AddApmModifier(ConfigModel config)
    {
        _logger.Verbose("Received request to add ApmModifier to Empty");
    }

    public void Dispose()
    {
        _fakeDataToSend = null;

        _fakeDataTimer?.Dispose();
        _fakeDataTimer = null;
    }

    public bool SetListening(bool state)
    {
        _logger.Verbose("Received listening state request for {state} to Empty", state);
        if (IsStarted)
        {
            IsListening = state;
        }
        else
        {
            IsListening = false;
        }
        return IsListening;
    }

    public Res Start()
    {
        _logger.Verbose("Starting fake audio data");

        var formatMulti = _format.Format switch
        {
            SampleFormat.U8 => 1,
            SampleFormat.S16 => 2,
            SampleFormat.S24 => 3,
            SampleFormat.S32 => 4,
            SampleFormat.F32 => 4,
            _ => 1
        };

        var bytesPer10Ms = _format.Channels * formatMulti * _format.SampleRate / 100;
        _fakeDataToSend = [.. new byte[bytesPer10Ms].Select(x => (byte)0)];

        _fakeDataTimer?.Stop();
        _fakeDataTimer?.Dispose();

        _fakeDataTimer = new(10)
        {
            AutoReset = true,
        };
        _fakeDataTimer.Elapsed += SendFakeData;
        _fakeDataTimer.Start();

        return ResC.Ok();
    }

    private void SendFakeData(object? sender, ElapsedEventArgs e)
    {
        if (_fakeDataToSend is not null)
        {
            OnAudioProcessed.Invoke(_fakeDataToSend, Capability.Record);
        }
    }

    public Res Stop()
    {
        _logger.Verbose("Stopping fake audio data");

        _fakeDataTimer?.Stop();
        _fakeDataTimer?.Dispose();
        _fakeDataTimer = null;

        _fakeDataToSend = null;

        return ResC.Ok();
    }
}