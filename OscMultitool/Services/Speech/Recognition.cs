using Hoscy.Ui.Pages;
using System;
using System.Linq;
using System.Text.RegularExpressions;

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

            UpdateDenoiseRegex();
            _recognizer.SpeechRecognized += OnSpeechRecognized;
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

        #region Result Handling
        /// <summary>
        /// Processing and sending off the message
        /// </summary>
        private static void OnSpeechRecognized(object? sender, string message)
        {
            var cleanedMessage = CleanMessage(message);

            if (string.IsNullOrWhiteSpace(cleanedMessage))
                return;

            var processor = new TextProcessor()
            {
                TriggerReplace = Config.Speech.UseReplacements,
                TriggerCommands = true,
                UseTextbox = Config.Speech.UseTextbox,
                UseTts = Config.Speech.UseTts,
                AllowTranslate = true
            };

            processor.Process(cleanedMessage);
        }

        private static Regex _denoiseFilter = new(" *");
        /// <summary>
        /// Removes "noise" from message
        /// </summary>
        private static string CleanMessage(string message)
        {
            message = message.Trim();
            if (Config.Speech.RemoveFullStop)
                message = message.TrimEnd('.');

            if (!_denoiseFilter.IsMatch(message))
                return string.Empty;

            message = _denoiseFilter.Match(message).Groups[1].Value.Trim();

            return message;
        }

        /// <summary>
        /// Generates a regex for denoising
        /// </summary>
        internal static void UpdateDenoiseRegex()
        {
            var filterWords = Config.Speech.NoiseFilter.Select(x => $"(?:{Regex.Escape(x)})");
            var filterCombined = string.Join('|', filterWords);
            var regString = $"^(?:(?<= |\\b)(?:{filterCombined})(?= |\\b))?(.*?)(?:(?<= |\\b)(?:{filterCombined})(?= |\\b))?$";
            Logger.PInfo($"Updated denoiser ({regString})");
            _denoiseFilter = new Regex(regString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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
