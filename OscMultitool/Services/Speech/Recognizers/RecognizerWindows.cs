using System;
using System.Speech.Recognition;

namespace Hoscy.Services.Speech.Recognizers
{
    internal class RecognizerWindows : RecognizerBase
    {
        new internal static RecognizerPerms Perms => new()
        {
            Description = "Recognizer using Windows Recognition, low quality, please avoid",
            UsesWinRecognizer = true
        };

        internal override bool IsListening => _isListening;

        private SpeechRecognitionEngine? _rec;
        private bool _isListening = false;

        protected override bool StartInternal()
        {
            try
            {
                try { _rec = new(Config.Speech.WinModelId); }
                catch { _rec = new(); }

                _rec.LoadGrammar(new DictationGrammar());
                _rec.SpeechRecognized += Recognizer_SpeechRecognized;
                _rec.SetInputToDefaultAudioDevice();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to start windows speech recognition.");
                return false;
            }
        }

        protected override bool SetListeningInternal(bool enabled)
        {
            if (_rec == null || _isListening == enabled)
                return false;

            _isListening = enabled;

            if (enabled)
                _rec.RecognizeAsync(RecognizeMode.Multiple);
            else
                _rec.RecognizeAsyncStop();

            Logger.PInfo("Microphone status changed to " + enabled);
            return true;
        }

        protected override void StopInternal()
        {
            _rec?.RecognizeAsyncStop();
            _rec?.Dispose();
            _rec = null;
        }

        private static void Recognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            Logger.Log("Got Message: " + e.Result.Text);

            var message = Denoise(e.Result.Text);
            if (string.IsNullOrWhiteSpace(message))
                return;

            ProcessMessage(message);
        }
    }
}