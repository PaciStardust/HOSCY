using System.Buffers;
using System.Runtime.InteropServices;
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

    public void Start()
    {
        if (IsStarted) return;

        _logger.Debug("Starting AudioCaptureDeviceProxy");
        _wrappedDevice.Start();
        _wrappedDevice.OnAudioProcessed += HandleAudioProcessed;
        IsStarted = true;
    }

    public void Stop()
    {
        if (!IsStarted) return;

        _logger.Debug("Stopping AudioCaptureDeviceProxy");
        SetListening(false);
        _wrappedDevice.Stop();
        _wrappedDevice.OnAudioProcessed -= HandleAudioProcessed;
        IsStarted = false;
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
        Stop();
        if (!_wrappedDevice.IsDisposed)
            _wrappedDevice.Dispose();
    }
}