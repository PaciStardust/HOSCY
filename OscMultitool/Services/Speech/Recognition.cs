using OscMultitool.Ui.Pages;
using System.Collections.Generic;
using System.Speech.Recognition;

namespace OscMultitool.Services.Speech
{
    public static class Recognition
    {
        private static RecognizerBase? _recognizer;
        public static bool IsRecognizerRunning => _recognizer?.IsRunning ?? false;
        public static bool IsRecognizerListening => _recognizer?.IsListening ?? false;

        #region Recognizer Control
        public static bool StartRecognizer()
        {
            PageInfo.UpdateMicStatus();

            if (_recognizer != null || IsRecognizerRunning)
            {
                Logger.Warning("Attempted to start recognizer while one already was initialized", "Speech");
                return true;
            }

            _recognizer = PageSpeech.GetRecognizerFromUi();
            if (_recognizer == null)
            {
                Logger.Error("Unable to grab Recognizer Type from UI, please open an issue on GitHub", "Speech");
                return false;
            }

            Logger.PInfo("Attempting to start recognizer...", "Speech");
            if (!_recognizer.Start())
            {
                Logger.Warning("Failed to start recognizer", "Speech");
                _recognizer = null;
                return false;
            }
            Logger.PInfo("Successfully started recognizer", "Speech");
            return true;
        }

        public static bool SetListening(bool enabled)
        {
            if (!IsRecognizerRunning)
                return false;

            var res = _recognizer?.SetListening(enabled) ?? false;
            PageInfo.UpdateMicStatus();
            return res;
        }

        public static void StopRecognizer()
        {
            if (_recognizer == null)
            {
                Logger.Warning("Attempted to stop recognizer while one wasnt running", "Speech");
                return;
            }

            _recognizer.Stop();
            _recognizer = null;
            PageInfo.UpdateMicStatus();
            Logger.PInfo("Successfully stopped recognizer", "Speech");
        }
        #endregion

        #region WinListeners
        public static IReadOnlyList<RecognizerInfo> WindowsRecognizers { get; private set; } = GetWindowsRecognizers();
        private static IReadOnlyList<RecognizerInfo> GetWindowsRecognizers()
        {
            Logger.Info("Getting installed Speech Recognizers", "Speech");
            return SpeechRecognitionEngine.InstalledRecognizers();
        }

        public static int GetWindowsListenerIndex(string id)
        {
            for (int i = 0; i < WindowsRecognizers.Count; i++)
            {
                if (WindowsRecognizers[i].Id == id)
                    return i;
            }
            return -1;
        }
        #endregion
    }
}
