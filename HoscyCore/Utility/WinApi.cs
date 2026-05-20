#if WINDOWS

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

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
}

#endif