using Hoscy.Ui.Pages;
using System;

namespace Hoscy.Services.Speech
{
    internal static class Recognition
    {
        private static RecognizerBase? _recognizer;
        internal static bool IsRunning => _recognizer?.IsRunning ?? false;
        internal static bool IsListening => _recognizer?.IsListening ?? false;

        #region Recognizer Control
        internal static bool StartRecognizer()
        {
            TriggerRecognitionChanged();

            if (_recognizer != null || IsRunning)
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
            if (!IsRunning)
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
            => HandleRecognitionChanged(null, new(IsRunning, IsListening));
        #endregion
    }

    internal class RecognitionChangedEventArgs : EventArgs
    {
        internal bool Listening { get; init; } = false;
        internal bool Running { get; init; } = false;

        internal RecognitionChangedEventArgs(bool running, bool listening)
        {
            Listening = listening;
            Running = running;
        }
    }
}
