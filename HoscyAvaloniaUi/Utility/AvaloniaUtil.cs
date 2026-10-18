using System.IO;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using HoscyAvaloniaUi.Services;
using HoscyCore.Services.Network;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.Utility;

public static class AvaloniaUtil
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

    public static async Task CheckForUpdates(IRepoService repoService, PopupWindowFactory popup) {
        
        var res = await repoService.CheckForUpdates();
        if (res is null) return;
        if (!res.IsOk)
        {
            popup.OpenNotification("Failed to Check Update", res.Msg.Message, true, true);
            return;
        }

        var title = res.Value.Title ?? $"Update: {res.Value.VersionNew}";
        var message = $"Version {res.Value.VersionCurrent} => {res.Value.VersionNew}";
        if (!string.IsNullOrWhiteSpace(res.Value.Text))
        {
            message += $"\n\n{res.Value.Text}";
        }

        popup.OpenNotification(title, message, false, true, customButton: ("Open Release Page", res.Value.Url));
    }
}