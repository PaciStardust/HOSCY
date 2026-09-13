using Avalonia.Input.Platform;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.Utility;

public static class ClipboardUtil
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
}