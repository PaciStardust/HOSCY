#if WINDOWS
#pragma warning disable CA1416 // Validate platform compatibility

using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using Serilog;

namespace HoscyCore.Utility;

public static class WinApi
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    /// <summary>
    /// Launches an error box on windows, any other platform is hacky
    /// (feel free to prove me otherwise)
    /// If you use this software on anything but windows I expect you also could figure this out without a message box :3 
    /// </summary>
    public static void ShowErrorBoxOnWindows(string message, string title = "HOSCY - Error")
    {
        _ = MessageBoxW(IntPtr.Zero, message, title, 0x10);
    }

    [DllImport("Kernel32")]
    private static extern void AllocConsole();

    /// <summary>
    ///  Opens a console in Windows, other OS should just launch over command line to have logging
    /// </summary>
    public static void OpenConsole()
    {
        AllocConsole();
    }

    public static Res<List<WindowsRecognizerInfo>> GetWindowsRecognizers(ILogger logger)
    {
        try
        {
            var res = SpeechRecognitionEngine.InstalledRecognizers()
                .Select(x => new WindowsRecognizerInfo(x.Name, x.Description, x.Id)).ToList();
            return ResC.TOk(res);
        }
        catch (Exception ex)
        {
            return ResC.TFailLog<List<WindowsRecognizerInfo>>("Failed to retrieve installed recognizers", logger, ex);
        }
    }
    public record WindowsRecognizerInfo(string Name, string Desc, string Id);

    public static Res<List<VoiceInfo>> GetWindowsVoices(ILogger logger)
    {
        try
        {
            using var synth = new SpeechSynthesizer();
            var infos = GetWindowsVoices(synth);
            return ResC.TOk(infos);
        }
        catch (Exception ex)
        {
            return ResC.TFailLog<List<VoiceInfo>>("Failed to retrieve installed voices", logger, ex);
        }
    }
    public static List<VoiceInfo> GetWindowsVoices(SpeechSynthesizer synth)
    {
        return synth.GetInstalledVoices()
            .Where(x => x.Enabled)
            .Select(x => x.VoiceInfo)
            .ToList();
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
#endif