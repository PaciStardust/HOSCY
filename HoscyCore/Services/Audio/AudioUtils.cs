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
    public const string DEVICE_NONE = "[No Device]";
    public const string DEVICE_DEFAULT = "[Default Device]";

    #region Audio Engine Devices (General)
    public static Res UpdateDeviceListForEngine(ILogger logger, AudioEngine? audioEngine)
    {
        if (audioEngine is null || audioEngine.IsDisposed)
            return ResC.FailLog("Audio devices could not be updated, engine is not available", logger);

        return ResC.WrapR(audioEngine.UpdateAudioDevicesInfo, "Failed to update audio devices", logger);
    }

    private static Res<T>? CreateDeviceForEngine<T>
    (
        ILogger logger, 
        AudioEngine? audioEngine, 
        string deviceType,
        Func<Res<DeviceInfo[]>> funcGetDeviceInfos,
        Func<T> funcGetEmptyDevice,
        Func<DeviceInfo,Res<T>> funcCreateDevice,
        string primaryDeviceName, 
        string fallbackDeviceName = "",
        bool finalEmptyNotDefault = false
    )
        where T: notnull
    {
        logger.Debug("Attempting to create {devType} device for primary=\"{primary}\" fallback=\"{fallback}\"",
            deviceType, primaryDeviceName, fallbackDeviceName);

        var updateRes = UpdateDeviceListForEngine(logger, audioEngine);
        if (!updateRes.IsOk) return ResC.TFail<T>(updateRes.Msg);

        var deviceInfos = ResC.TWrap(() => funcGetDeviceInfos(), $"Failed to get {deviceType} device infos", logger);
        if (!deviceInfos.IsOk) return ResC.TFail<T>(deviceInfos.Msg);

        if (IsNoneDevice(primaryDeviceName))
        {
            logger.Debug("Primary {devType} device name is empty device", deviceType);
            return ResC.TOk<T>(funcGetEmptyDevice());
        }

        var (deviceInfo, defaultNull) = GetDeviceInfoForNameFull(logger, deviceType, deviceInfos.Value, primaryDeviceName, false);
        if (deviceInfo is null)
        {
            if (IsNoneDevice(fallbackDeviceName))
            {
                logger.Debug("Fallback {devType} device name is empty device", deviceType);
                return ResC.TOk(funcGetEmptyDevice());
            }

            (deviceInfo, defaultNull) = GetDeviceInfoForNameFull(logger, deviceType, deviceInfos.Value, fallbackDeviceName, defaultNull);
            if (deviceInfo is null)
            {
                logger.Debug("No {devType} devices found for fallback or empty, looking for default", deviceType);
                deviceInfo = defaultNull ? null : GetDefaultDevice(logger, deviceType, deviceInfos.Value);

                if (deviceInfo is null)
                {
                    logger.Warning("No {devType} device found, returning final null", deviceType);
                    return null;
                }
                else if (finalEmptyNotDefault)
                {
                    logger.Warning("No {devType} device found, returning empty by request", deviceType);
                    return ResC.TOk(funcGetEmptyDevice());
                }
            }
        }

        logger.Debug("Creating {devType} device for device {devName}", deviceType, deviceInfo.Value.Name);
        return ResC.TWrap(() => funcCreateDevice(deviceInfo.Value),
            $"Failed initializing {deviceType} device {deviceInfo.Value.Name}", logger);
    }

    private static (DeviceInfo? Info, bool DefaultNull) GetDeviceInfoForNameFull(ILogger? logger, string deviceType, DeviceInfo[] infos, string deviceName, bool defaultNull)
    {
        logger?.Debug("Locating {devType} device for name \"{name}\"", deviceType, deviceName);

        if (string.IsNullOrWhiteSpace(deviceName)) return (null, defaultNull);

        if (IsNoneDevice(deviceName))
        {
            logger?.Debug("None device received, returning null");
            return (null, defaultNull);
        }

        if (IsDefaultDevice(deviceName)) {
            if (defaultNull) return (null, defaultNull);
            var defDev = GetDefaultDevice(logger, deviceType, infos);
            return (defDev, defDev is null);
        };

        var matches = infos.Where(x => x.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length > 0)
        {
            if (matches.Length > 1)
            {
                logger?.Warning("Multiple {devType} devices found for name, picking first", deviceType);
            }
            var match = matches[0];
            logger?.Debug("Located {devType} device with name \"{name}\"", deviceType, match.Name);
            return (match, defaultNull);
        }
        logger?.Warning("Failed locating {devType} device for name \"{name}\"", deviceType, deviceName);

        return (GetDeviceInfoForName(logger, deviceType, infos, deviceName, false), defaultNull);
    }

    private static DeviceInfo? GetDefaultDevice(ILogger? logger, string deviceType, DeviceInfo[] infos)
    {
        var matches =  infos.Where(x => x.IsDefault).ToArray();
        if (matches.Length > 0)
        {
            if (matches.Length > 1)
            {
                logger?.Warning("Multiple default {devType} devices found, picking first", deviceType);
            }
            var match = matches[0];
            logger?.Debug("Located default {devType} device with name \"{name}\"", deviceType, match.Name);
            return match;
        }
        return null;
    }

    public static bool IsDefaultDevice(string deviceName)
    {
        return deviceName.Equals(DEVICE_DEFAULT, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsNoneDevice(string deviceName)
    {
        return deviceName.Equals(DEVICE_NONE, StringComparison.OrdinalIgnoreCase);
    }

    public static DeviceInfo? GetDeviceInfoForName(ILogger? logger, string deviceType, DeviceInfo[] infos, string deviceName, bool logVerbose = true)
    {
        if (logVerbose)
        {
            logger?.Debug("Locating {devType} device for name \"{name}\"", deviceType, deviceName);
        }

        var matches = infos.Where(x => x.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length > 0)
        {
            if (matches.Length > 1)
            {
                logger?.Warning("Multiple {devType} devices found for name, picking first", deviceType);
            }
            var match = matches[0];
            logger?.Debug("Located {devType} device with name \"{name}\"", deviceType, match.Name);
            return match;
        }
        logger?.Warning("Failed to locate a default {devType} device", deviceType);
        return null;
    }

    private static DeviceInfo? GetDeviceInfoForNamesFull(ILogger logger, string deviceType, DeviceInfo[] infos, string primaryDeviceName, string fallbackDeviceName)
    {
        logger.Debug("Attempting to find {devType} Device with name \"{primary}\" or \"{secondary}\"",
            deviceType, primaryDeviceName, fallbackDeviceName);

        var (device, defaultNull) = GetDeviceInfoForNameFull(logger, deviceType, infos, primaryDeviceName, false);
        if (device is not null) return device;

        (device, _) = GetDeviceInfoForNameFull(logger, deviceType, infos, fallbackDeviceName, defaultNull);
        if (device is null)
        {
            logger.Warning("Failed to find {devType} Device with name \"{primary}\" or \"{secondary}\"",
                deviceType, primaryDeviceName, fallbackDeviceName);
        }
        
        return device;
    }
    #endregion

    #region Audio Engine Devices (Capture)
    public static Res<DeviceInfo[]> GetCaptureInfosForEngine(ILogger logger, AudioEngine? audioEngine)
    {
        return audioEngine is not null && !audioEngine.IsDisposed 
            ? ResC.TOk(audioEngine.CaptureDevices)
            : ResC.TFailLog<DeviceInfo[]>("Failed to retrieve capture devices, audio engine not available", logger);
    }

    public static Res<IAudioCaptureDeviceProxy>? CreateCaptureForEngine
    (ILogger logger, ILogger devLogger, AudioEngine? audioEngine, string primaryDeviceName, string fallbackDeviceName = "", bool finalEmptyNotDefault = false, AudioFormat? format = null)
    {
        format ??= new AudioFormat
        {
            SampleRate = 16000,
            Channels = 1,
            Format = SampleFormat.S16
        };

        return CreateDeviceForEngine
        (
            logger,
            audioEngine,
            "capture",
            () => GetCaptureInfosForEngine(logger, audioEngine),
            () => new EmptyAudioCaptureDeviceProxy(devLogger, format.Value),
            (deviceInfo) =>
            {
                var device = audioEngine!.InitializeCaptureDevice(deviceInfo, format.Value);
                logger.Debug("Created capture device for device {devName}", deviceInfo.Name);
                return ResC.TOk<IAudioCaptureDeviceProxy>(new AudioCaptureDeviceProxy(device, devLogger));
            },
            primaryDeviceName,
            fallbackDeviceName,
            finalEmptyNotDefault
        );
    }

    public static Res<DeviceInfo>? GetCaptureInfoForNamesForEngine(ILogger logger, AudioEngine? audioEngine, string primaryDeviceName, string fallbackDeviceName = "")
    {
        var devices = GetCaptureInfosForEngine(logger, audioEngine);
        if (!devices.IsOk) return ResC.TFail<DeviceInfo>(devices.Msg);

        var search = GetDeviceInfoForNamesFull(logger, "capture", devices.Value, primaryDeviceName, fallbackDeviceName);
        return search is null ? null : ResC.TOk(search.Value);
    }
    #endregion

    #region Audio Engine Devices (Playback)
    public static Res<DeviceInfo[]> GetPlaybackInfosForEngine(ILogger logger, AudioEngine? audioEngine)
    {
        return audioEngine is not null && !audioEngine.IsDisposed 
            ? ResC.TOk(audioEngine.PlaybackDevices)
            : ResC.TFailLog<DeviceInfo[]>("Failed to retrieve playback devices, audio engine not available", logger);
    }

    public static Res<IAudioPlaybackDeviceProxy>? CreatePlaybackForEngine
        (ILogger logger, ILogger devLogger, AudioEngine? audioEngine, string primaryDeviceName, string fallbackDeviceName = "", bool finalEmptyNotDefault = false, AudioFormat? format = null)
    {
        format ??= new AudioFormat
        {
            SampleRate = 16000,
            Channels = 1,
            Format = SampleFormat.S16
        };

        return CreateDeviceForEngine
        (
            logger,
            audioEngine,
            "playback",
            () => GetPlaybackInfosForEngine(logger, audioEngine),
            () => new EmptyAudioPlaybackDeviceProxy(devLogger),
            (deviceInfo) =>
            {
                var device = audioEngine!.InitializePlaybackDevice(deviceInfo, format: format.Value);
                logger.Debug("Created playback device for device {devName}", deviceInfo.Name);
                return ResC.TOk<IAudioPlaybackDeviceProxy>(new AudioPlaybackDeviceProxy(device, devLogger));
            },
            primaryDeviceName,
            fallbackDeviceName,
            finalEmptyNotDefault
        );
    }

    public static Res<DeviceInfo>? GetPlaybackInfoForNamesForEngine(ILogger logger, AudioEngine? audioEngine, string primaryDeviceName, string fallbackDeviceName = "")
    {
        var devices = GetPlaybackInfosForEngine(logger, audioEngine);
        if (!devices.IsOk) return ResC.TFail<DeviceInfo>(devices.Msg);

        var search = GetDeviceInfoForNamesFull(logger, "playback", devices.Value, primaryDeviceName, fallbackDeviceName);
        return search is null ? null : ResC.TOk(search.Value);
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