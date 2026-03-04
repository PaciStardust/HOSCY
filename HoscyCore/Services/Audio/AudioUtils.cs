using System.Runtime.InteropServices;
using Serilog;
using SoundFlow.Abstracts;
using SoundFlow.Structs;

namespace HoscyCore.Services.Audio;

public static class AudioUtils
{
    public static DeviceInfo? FindDevice(DeviceInfo[]? devices, string configId, ILogger logger)
    {
        if (devices is null || devices.Length == 0)
        {
            logger.Warning("No audio devices provided for search, returning null");
            return null;
        }

        var configMatches = devices.Where(x => x.Name.ToString() == configId).ToArray();
        
        if (configMatches.Length == 0)
        {
            logger.Warning("No audio device found for configuired id {configId}, picking default instead", configId);
        } 
        else 
        {
            if (configMatches.Length > 1)
            {
                logger.Warning("More than one audio device found for id {configId}, picking first", configId);
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

    public static void ConvertLinearFloatsToPcmBytes(Span<float> samplesIn, Span<byte> bytesOut)
    {
        var shortView = MemoryMarshal.Cast<byte, short>(bytesOut);
        
        for (var i = 0; i < samplesIn.Length; i++)
        {
            float clamped = Math.Max(-1.0f, Math.Min(1.0f, samplesIn[i]));
            shortView[i] = (short)(clamped * 32767f);
        }
    }
}