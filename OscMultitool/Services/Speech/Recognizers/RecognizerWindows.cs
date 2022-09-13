using System;
using System.Speech.Recognition;

namespace OscMultitool.Services.Speech.Recognizers
{
    public class RecognizerWindows : RecognizerBase
    {
        new public static RecognizerPerms Perms => new()
        {
            UsesWinRecognizer = true
        };

        public override bool IsListening => _isListening;

        private SpeechRecognitionEngine? _rec;
        private bool _isListening = false;

        protected override bool StartInternal()
        {
            try
            {
                _rec = new SpeechRecognitionEngine(Config.Speech.WinModelId);
                _rec.LoadGrammar(new DictationGrammar());
                _rec.SpeechRecognized += Recognizer_SpeechRecognized;
                _rec.SetInputToDefaultAudioDevice();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "RecWin");
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

            Logger.PInfo("Microphone status changed to " + enabled, "RecWin");
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
            Logger.Log("Got Message: " + e.Result.Text, "RecWin");

            var message = Denoise(e.Result.Text);
            if (string.IsNullOrWhiteSpace(message))
                return;

            ProcessMessage(message);
        }
    }
}