using Hoscy.Services.Speech.Utilities;
using NAudio.Wave;
using System;
using System.Speech.Recognition;

namespace Hoscy.Services.Speech.Recognizers
{
    public class RecognizerWindowsV2 : RecognizerBase
    {
        new public static RecognizerPerms Perms => new()
        {
            Description = "Recognizer using Windows Recognition, low quality, please avoid",
            UsesMicrophone = true,
            UsesWinRecognizer = true
        };

        public override bool IsListening => _microphone.IsListening;

        private SpeechRecognitionEngine? _rec;
        private readonly WaveInProxy _microphone = new();
        protected readonly SpeechStreamer _stream = new(12800);

        #region Starting / Stopping
        protected override bool StartInternal()
        {
            try
            {
                try { _rec = new(Config.Speech.WinModelId); }
                catch { _rec = new(); }

                _rec.LoadGrammar(new DictationGrammar());
                _rec.SpeechRecognized += OnSpeechRecognized;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to start windows speech recognition.");
                return false;
            }

            _microphone.DataAvailable += OnDataAvailable;
            _microphone.Start();

            _rec.SetInputToAudioStream(_stream, new(16000, System.Speech.AudioFormat.AudioBitsPerSample.Sixteen, System.Speech.AudioFormat.AudioChannel.Mono));
            _rec.RecognizeAsync(RecognizeMode.Multiple);
            return true;
        }

        protected override void StopInternal()
        {
            _microphone.Dispose();
            _stream.Dispose();
        }

        protected override bool SetListeningInternal(bool enabled)
            => _microphone.SetMuteStatus(enabled);
        #endregion

        #region Events
        private void OnDataAvailable(object? sender, WaveInEventArgs e)
            => _stream.Write(e.Buffer, 0, e.BytesRecorded);

        private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            Logger.Log("Got Message: " + e.Result.Text);

            var message = Denoise(e.Result.Text);
            if (string.IsNullOrWhiteSpace(message))
                return;

            ProcessMessage(message);
        }
        #endregion
    }
}