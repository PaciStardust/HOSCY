using Hoscy.Services.Speech;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Hoscy.Services.Api
{
    public static class Hotkeys
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        private const int HOTKEY_ID = 4096;
        private const uint KEY_ALT = 0x0001;
        private const uint KEY_M = 0x4D;

        private static HwndSource? _source;

        /// <summary>
        /// Registers all the keybinds
        /// </summary>
        public static void Register()
        {
            IntPtr handle = new WindowInteropHelper(App.Current.MainWindow).Handle;
            _source = HwndSource.FromHwnd(handle);
            _source.AddHook(KeyHandler);

            Logger.PInfo("Registering hotkeys");

            RegisterHotKey(handle, HOTKEY_ID, KEY_ALT, KEY_M);
        }

        /// <summary>
        /// Handles all input keys
        /// </summary>
        private static IntPtr KeyHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == KEY_M)
                            {
                                Logger.Log("Mute keybind has been hit");
                                Recognition.SetListening(!Recognition.IsRecognizerListening);
                            }
                            //handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

    }
}
