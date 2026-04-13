using System.Buffers;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Enums;

namespace HoscyCore.Services.Audio;

public class AudioCaptureDeviceProxy(AudioCaptureDevice wrappedDevice, ILogger logger) : IDisposable
{
    private readonly AudioCaptureDevice _wrappedDevice = wrappedDevice;
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
        if (!_wrappedDevice.IsDisposed)
            _wrappedDevice.Dispose();
    }
}