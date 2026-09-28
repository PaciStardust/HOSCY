using System.IO;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.Utility;

public static class AvaloniaUtil //todo: audio quality for API/piper, use swappable for voice, backup device, empty device, missing device in dropdown
{
    public static void CopyToClipboard(ILogger? logger, IClipboard? clipboard, string text)
    {
        logger?.Debug("Received clipboard copy request");
        
        if (clipboard is null)
        {
            logger?.Warning("Clipboard copy request failed, no clipboard available");
            return;
        }

        var res = ResC.WrapR(clipboard.SetTextAsync(text).AsSync, "Clipboard copy failed", logger);
        if (res.IsOk)
        {
            logger?.Debug("Clipboard copy request succeeded");
        }
    }

    public static Res<Stream> GetAvaloniaResource(string path, ILogger logger)
    {
        logger.Debug("Retrieving avalonia resource \"{path}\"", path);
        return ResC.TWrapR(() => AssetLoader.Open(new(path)),
            $"Failed to retrieve avalonia resource \"{path}\"", logger);
    }
}