using System.Buffers;
using System.Runtime.InteropServices;
using EchoSharp.Audio;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Enums;

namespace HoscyWhisperV2Process;

public class HoscyAvailableAudioSource : AwaitableWaveFileSource
{
    private readonly AudioCaptureDevice _capture;

    public HoscyAvailableAudioSource(AudioCaptureDevice capture, IChannelAggregationStrategy? aggregationStrategy = null)
        : base (true, false, DefaultInitialSize, DefaultInitialSize, aggregationStrategy)
    {
        _capture = capture;

        Initialize(new AudioSourceHeader()
        {
            BitsPerSample = _capture.Format.Format == SampleFormat.S16 ? (ushort)16 : throw new Exception("Only S16 sample format supported"),
            Channels = _capture.Format.Channels == 1 ? (ushort)1 : throw new Exception("Only mono channel supported"),
            SampleRate = (uint)_capture.Format.SampleRate
        });

        _capture.OnAudioProcessed += OnAudioProcessed;
    }

    public void StartRecording()
    {
        _capture.Start();
    }

    public void StopRecording()
    {
        _capture.Stop();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _capture.OnAudioProcessed -= OnAudioProcessed;
        }
        base.Dispose(disposing);
    }

    private void OnAudioProcessed(Span<float> samples, Capability capability)
    {
        var byteLen = samples.Length * 2;
        
        byte[] rentedBytes = ArrayPool<byte>.Shared.Rent(byteLen);
        try
        {
            var shortView = MemoryMarshal.Cast<byte, short>(rentedBytes.AsSpan());
        
            for (var i = 0; i < samples.Length; i++)
            {
                float clamped = Math.Max(-1.0f, Math.Min(1.0f, samples[i]));
                shortView[i] = (short)(clamped * 32767f);
            }

            WriteData(rentedBytes.AsMemory(0, byteLen));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBytes);
        }
    }
}