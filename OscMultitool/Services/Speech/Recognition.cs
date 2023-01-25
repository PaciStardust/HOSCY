using Hoscy.Ui.Pages;
using System;
using System.Collections.Generic;
using System.Speech.Recognition;

namespace Hoscy.Services.Speech
{
    internal static class Recognition
    {
        private static RecognizerBase? _recognizer;
        internal static bool IsRecognizerRunning => _recognizer?.IsRunning ?? false;
        internal static bool IsRecognizerListening => _recognizer?.IsListening ?? false;

        #region Recognizer Control
        internal static bool StartRecognizer()
        {
            TriggerRecognitionChanged();

            if (_recognizer != null || IsRecognizerRunning)
            {
                Logger.Warning("Attempted to start recognizer while one already was initialized");
                return true;
            }

            _recognizer = PageSpeech.GetRecognizerFromUi();
            if (_recognizer == null)
            {
                Logger.Error("Unable to grab Recognizer Type from UI, please open an issue on GitHub");
                return false;
            }

            Config.SaveConfig(); //Saving to ensure it doesnt wipe when crashing
            Logger.PInfo("Attempting to start recognizer...");
            if (!_recognizer.Start())
            {
                Logger.Warning("Failed to start recognizer");
                _recognizer = null;
                return false;
            }

            Logger.PInfo("Successfully started recognizer");
            TriggerRecognitionChanged();
            return true;
        }

        internal static bool SetListening(bool enabled)
        {
            if (!IsRecognizerRunning)
                return false;

            var res = _recognizer?.SetListening(enabled) ?? false;
            TriggerRecognitionChanged();
            return res;
        }

        internal static void StopRecognizer()
        {
            if (_recognizer == null)
            {
                Logger.Warning("Attempted to stop recognizer while one wasnt running");
                return;
            }

            Config.SaveConfig(); //Saving to ensure it doesnt wipe when crashing
            _recognizer.Stop();
            _recognizer = null;
            TriggerRecognitionChanged();
            Logger.PInfo("Successfully stopped recognizer");
        }
        #endregion

        #region Events
        internal static event EventHandler<RecognitionChangedEventArgs> RecognitionChanged = delegate { };

        private static void HandleRecognitionChanged(object? sender, RecognitionChangedEventArgs e)
            => RecognitionChanged.Invoke(sender, e);

        private static void TriggerRecognitionChanged()
            => HandleRecognitionChanged(null, new(IsRecognizerRunning, IsRecognizerListening));
        #endregion

        #region WinListeners
        internal static IReadOnlyList<RecognizerInfo> WindowsRecognizers { get; private set; } = GetWindowsRecognizers();
        private static IReadOnlyList<RecognizerInfo> GetWindowsRecognizers()
        {
            Logger.Info("Getting installed Speech Recognizers");
            return SpeechRecognitionEngine.InstalledRecognizers();
        }

        internal static int GetWindowsListenerIndex(string id)
        {
            for (int i = 0; i < WindowsRecognizers.Count; i++)
            {
                if (WindowsRecognizers[i].Id == id)
                    return i;
            }

            if (WindowsRecognizers.Count == 0)
                return -1;
            else
                return 0;
        }
        #endregion
    }

    internal class RecognitionChangedEventArgs : EventArgs
    {
        internal bool Listening { get; init; }
        internal bool Running { get; init; }

        internal RecognitionChangedEventArgs(bool running, bool listening)
        {
            Listening = listening;
            Running = running;
        }
    }
}
