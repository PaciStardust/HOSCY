using System.Buffers;
using HoscyCore.Configuration.Modern;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Enums;
using SoundFlow.Extensions.WebRtc.Apm;
using SoundFlow.Extensions.WebRtc.Apm.Modifiers;

namespace HoscyCore.Services.Audio;

public class AudioCaptureDeviceProxy(AudioCaptureDevice wrappedDevice, ILogger logger) : IDisposable
{
    private readonly AudioCaptureDevice _wrappedDevice = wrappedDevice;
    private WebRtcApmModifier? _apmModifier = null; 
    private readonly ILogger _logger = logger.ForContext<AudioCaptureDeviceProxy>();

    public bool IsStarted { get; private set; } = false;
    public bool IsListening { get; private set; } = false;

    public Res Start()
    {
        if (IsStarted) return ResC.Ok();

        _logger.Debug("Starting AudioCaptureDeviceProxy");
        var res = ResC.WrapR(_wrappedDevice.Start, "Failed to start inner audio device", _logger);

        if (res.IsOk)
        {
            _wrappedDevice.OnAudioProcessed += HandleAudioProcessed;
            IsStarted = true;
        }
        return res;
    }

    public Res Stop()
    {
        if (!IsStarted) return ResC.Ok();

        _logger.Debug("Stopping AudioCaptureDeviceProxy");
        SetListening(false);

        var res = ResC.WrapR(_wrappedDevice.Stop, "Failed to stop inner audio device", _logger);

        if (res.IsOk)
        {
            IsStarted = false;
        }
        _wrappedDevice.OnAudioProcessed -= HandleAudioProcessed;
        return res;
    }

    public bool SetListening(bool state)
    {
        if (!IsStarted) return false;

        _logger.Debug("Set AudioCaptureDeviceProxy to listening={state}", state);
        IsListening = state;

        return IsListening;
    }

    public void AddApmModifier(bool echoCancellation, int ecLatencyMs, bool noiseSuppression, NoiseSuppressionLevel nsLevel,
        bool gainControl, bool highPass, bool preAmp, float preAmpGain)
    {
        if (!CanInitApmModifier()) return;

        _apmModifier = AudioUtils.AddWebRtcModifier(
            _wrappedDevice,
            echoCancellation,
            ecLatencyMs,
            noiseSuppression,
            nsLevel,
            gainControl,
            highPass,
            preAmp,
            preAmpGain);
    }
    public void AddApmModifier(ConfigModel config)
    {
        if (!CanInitApmModifier()) return;

        _apmModifier = AudioUtils.AddWebRtcModifier(_wrappedDevice, config);
    }
    private bool CanInitApmModifier()
    {
        _logger.Debug("Adding ApmModifier to proxy");

        if (_apmModifier is not null)
        {
            _logger.Warning("Attempted to set ApmModifier when it was already set");
            return false;
        }

        return true;
    }

    public event Action<Span<byte>, Capability> OnAudioProcessed = delegate { };
    private byte[]? _emptyBytes = null;
    private void HandleAudioProcessed(Span<float> samples, Capability capability)
    {
        if (!IsStarted) return;

        var byteLen = samples.Length * 2;
                
        if (!IsListening) {
            if (_emptyBytes is null || _emptyBytes.Length < byteLen)
            {
                _emptyBytes = new byte[byteLen];
            }
            OnAudioProcessed.Invoke(_emptyBytes.AsSpan(0, byteLen), capability);
            return;
        }

        _apmModifier?.Process(samples, 1);

        byte[] rentedBytes = ArrayPool<byte>.Shared.Rent(byteLen);
        try
        {
            AudioUtils.ConvertLinearFloatsToPcmBytes(samples, rentedBytes.AsSpan());
            OnAudioProcessed.Invoke(rentedBytes.AsSpan(0, byteLen), capability);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBytes);
        }
    }

    public void Dispose()
    {
        Stop(); //Result does not matter

        _apmModifier?.Dispose();
        _apmModifier = null;

        if (!_wrappedDevice.IsDisposed)
        {
            _wrappedDevice.Dispose();
        }
    }
}