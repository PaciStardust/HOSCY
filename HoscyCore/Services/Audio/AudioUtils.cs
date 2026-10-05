using System.Buffers.Binary;
using System.Runtime.InteropServices;
using HoscyCore.Configuration.Modern;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Enums;
using SoundFlow.Extensions.WebRtc.Apm;
using SoundFlow.Extensions.WebRtc.Apm.Modifiers;
using SoundFlow.Structs;

namespace HoscyCore.Services.Audio;

public static class AudioUtils
{

    public const string EMPTY_DEVICE = "[No Device]";

    #region Audio Engine Devices (General)
    public static Res UpdateDeviceListForEngine(ILogger logger, AudioEngine? audioEngine)
    {
        if (audioEngine is null || audioEngine.IsDisposed)
            return ResC.FailLog("Audio devices could not be updated, engine is not available", logger);

        return ResC.WrapR(audioEngine.UpdateAudioDevicesInfo, "Failed to update audio devices", logger);
    }

    public static DeviceInfo? FindDevice(ILogger logger, DeviceInfo[]? devices, string deviceName)
    {
        if (devices is null || devices.Length == 0)
        {
            logger.Warning("No audio devices provided for search, returning null");
            return null;
        }

        var configMatches = devices.Where(x => x.Name.ToString() == deviceName).ToArray();
        
        if (configMatches.Length == 0)
        {
            logger.Warning("No audio device found for configuired id {configId}, picking default instead", deviceName);
        } 
        else 
        {
            if (configMatches.Length > 1)
            {
                logger.Warning("More than one audio device found for id {configId}, picking first", deviceName);
            }
            return configMatches[0];
        }

        var defaultMatches = devices.Where(x => x.IsDefault).ToArray();

        if (defaultMatches.Length == 0)
        {
            logger.Warning("No default audio device found, picking first instead");
            return devices[0];
        } 
        else 
        {
            if (defaultMatches.Length > 1)
            {
                logger.Warning("More than one default audo device found, picking first");
            }
            return defaultMatches[0];
        }
    }

    //todo: needed?
    public static Res<DeviceInfo>? FindDeviceWithChecks
        (ILogger logger, DeviceInfo[] devices, string deviceName, string deviceTypeForLog, AudioEngine? audioEngine)
    {
        if (audioEngine is null || audioEngine.IsDisposed)
            return ResC.TFailLog<DeviceInfo>($"Unable to retrieve {deviceTypeForLog} device, audio engine is not available", logger);
        
        var devInfo = FindDevice(logger, devices, deviceName);
        if (!devInfo.HasValue)
        {
            logger.Error("Unable to retrieve {deviceTypeForLog} device, none found", deviceTypeForLog);
        }

        return devInfo.HasValue ? ResC.TOk(devInfo.Value) : null;
    }
    #endregion

    #region Audio Engine Devices (Capture)
    public static Res<DeviceInfo[]> GetCaptureDevicesForEngine(ILogger logger, AudioEngine? audioEngine)
    {
        return audioEngine is not null && !audioEngine.IsDisposed 
            ? ResC.TOk(audioEngine.CaptureDevices)
            : ResC.TFailLog<DeviceInfo[]>("Failed to retrieve capture devices, audio engine not available", logger);
    }

    //todo: Remove, only proxy exposed
    public static Res<AudioCaptureDevice>? CreateCaptureDeviceForEngine
        (ILogger logger, AudioEngine? audioEngine, string deviceName, AudioFormat? format = null)
    {
        var updateRes = UpdateDeviceListForEngine(logger, audioEngine);
        if (!updateRes.IsOk) return ResC.TFail<AudioCaptureDevice>(updateRes.Msg);

        var deviceInfos = GetCaptureDevicesForEngine(logger, audioEngine);
        if (!deviceInfos.IsOk) return ResC.TFail<AudioCaptureDevice>(deviceInfos.Msg);

        var deviceInfo = FindDeviceWithChecks(logger, deviceInfos.Value, deviceName, "capture", audioEngine);
        if (deviceInfo is null) return null;
        if (!deviceInfo.IsOk) return ResC.TFail<AudioCaptureDevice>(deviceInfo.Msg);

        format ??= new AudioFormat
        {
            SampleRate = 16000,
            Channels = 1,
            Format = SampleFormat.S16
        };

        logger.Debug("Creating capture device for device {devName}", deviceInfo.Value.Name);
        return ResC.TWrap(() =>
        {
            var device = audioEngine!.InitializeCaptureDevice(deviceInfo.Value, format.Value);
            logger.Debug("Created capture device for device {devName}", deviceInfo.Value.Name);
            return ResC.TOk(device);
        }, $"Failed initializing capture device {deviceInfo.Value.Name}", logger);
    }

    public static Res<IAudioCaptureDeviceProxy>? CreateCaptureDeviceProxyForEngine
        (ILogger logger, AudioEngine? audioEngine, string deviceName, AudioFormat? format = null)
    {
        var dev = CreateCaptureDeviceForEngine(logger, audioEngine, deviceName, format);
        if (dev is null) return null;
        return dev.IsOk
            ? ResC.TOk<IAudioCaptureDeviceProxy>(new AudioCaptureDeviceProxy(dev.Value, logger))
            : ResC.TFail<IAudioCaptureDeviceProxy>(dev.Msg);
    }
    #endregion

    #region Audio Engine Devices (Playback)
    public static Res<DeviceInfo[]> GetPlaybackDevicesForEngine(ILogger logger, AudioEngine? audioEngine)
    {
        return audioEngine is not null && !audioEngine.IsDisposed 
            ? ResC.TOk(audioEngine.PlaybackDevices)
            : ResC.TFailLog<DeviceInfo[]>("Failed to retrieve playback devices, audio engine not available", logger);
    }

    public static Res<IAudioPlaybackDeviceProxy>? CreatePlaybackDeviceProxyForEngine
        (ILogger logger, AudioEngine? audioEngine, string deviceName, AudioFormat? format = null)
    {
        var updateRes = UpdateDeviceListForEngine(logger, audioEngine);
        if (!updateRes.IsOk) return ResC.TFail<IAudioPlaybackDeviceProxy>(updateRes.Msg);

        var deviceInfos = GetPlaybackDevicesForEngine(logger, audioEngine);
        if (!deviceInfos.IsOk) return ResC.TFail<IAudioPlaybackDeviceProxy>(deviceInfos.Msg);

        var deviceInfo = FindDeviceWithChecks(logger, deviceInfos.Value, deviceName, "playback", audioEngine);
        if (deviceInfo is null) return null;
        if (!deviceInfo.IsOk) return ResC.TFail<IAudioPlaybackDeviceProxy>(deviceInfo.Msg);

        format ??= new AudioFormat
        {
            SampleRate = 16000,
            Channels = 1,
            Format = SampleFormat.S16
        };

        logger.Debug("Creating playback device for device {devName}", deviceInfo.Value.Name);
        return ResC.TWrap(() =>
        {
            var device = audioEngine!.InitializePlaybackDevice(deviceInfo.Value, format.Value);
            logger.Debug("Created playback device for device {devName}", deviceInfo.Value.Name);
            return ResC.TOk<IAudioPlaybackDeviceProxy>(new AudioPlaybackDeviceProxy(device, logger));
        }, $"Failed initializing playback device {deviceInfo.Value.Name}", logger);
    }
    #endregion

    #region WebRtc
    public static WebRtcApmModifier AddWebRtcModifier(AudioDevice device, bool echoCancellation, int ecLatencyMs, bool noiseSuppression, NoiseSuppressionLevel nsLevel,
        bool gainControl, bool highPass, bool preAmp, float preAmpGain)
    {
        var apmModifier = new WebRtcApmModifier(
            device,
            aecEnabled: echoCancellation,
            aecMobileMode: false,
            aecLatencyMs: ecLatencyMs.MinMax(0, 1000),
            nsEnabled: noiseSuppression,
            nsLevel: nsLevel,
            agc1Enabled: false,
            agc2Enabled: gainControl,
            hpfEnabled: highPass,
            preAmpEnabled: preAmp,
            preAmpGain: preAmpGain,
            useMultichannelCapture: false,
            useMultichannelRender: false,
            downmixMethod: DownmixMethod.AverageChannels
        );
        return apmModifier; //todo: [TEST] Does this apply like this or is additional config needed?
    }
    public static WebRtcApmModifier AddWebRtcModifier(AudioDevice device, ConfigModel config)
    {
        return AddWebRtcModifier
        (
            device,
            config.WebRtc_UseEchoCancellation,
            config.WebRtc_EchoCancellationDelayMs,
            config.WebRtc_UseNoiseSuppression,
            config.WebRtc_NoiseSuppressionLevel,
            config.WebRtc_UseAutomaticGainControl,
            config.WebRtc_UseHighPassFilter,
            config.WebRtc_UsePreAmplifier,
            config.WebRtc_PreAmplifierGainFactor
        );
    }
    #endregion

    #region Bytes and Wav
    public static void ConvertLinearFloatsToPcmBytes(Span<float> samplesIn, Span<byte> bytesOut)
    {
        var shortView = MemoryMarshal.Cast<byte, short>(bytesOut);
        
        for (var i = 0; i < samplesIn.Length; i++)
        {
            float clamped = Math.Max(-1.0f, Math.Min(1.0f, samplesIn[i]));
            shortView[i] = (short)(clamped * 32767f);
        }
    }

    public static readonly byte[] BaseWavHeader = CreateBaseWavHeader();
    private static byte[] CreateBaseWavHeader()
    {
        byte[] header = [
            (byte)'R', (byte)'I', (byte)'F', (byte)'F',
            0, 0, 0, 0, // File size tbd
            (byte)'W', (byte)'A', (byte)'V', (byte)'E',
            (byte)'f', (byte)'m', (byte)'t', (byte)' ',
            16, 0, 0, 0, // Format data len
            1, 0, 1, 0, // PCM / Channel
            0, 0, 0, 0, // Sample rate tdb
            0, 0, 0, 0, // Byte rate tbd
            2, 0, 16, 0, // Block size, Bits per sample
            (byte)'d', (byte)'a', (byte)'t', (byte)'a',
            0, 0, 0, 0 // Data size tbd
        ];

        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(24), 16_000); // Sample Rate
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(28), 32_000); // Byte Rate

        return header;
    }

    public static void WriteRestOfWavHeader(Span<byte> dataWithHeader)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(dataWithHeader.Slice(4, 4), (uint)dataWithHeader.Length - 8);
        BinaryPrimitives.WriteUInt32LittleEndian(dataWithHeader.Slice(40, 4), (uint)dataWithHeader.Length - 44);
    }
    #endregion
}