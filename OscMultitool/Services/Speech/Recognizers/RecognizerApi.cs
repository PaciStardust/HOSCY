using Hoscy;
using NAudio.Wave;
using Hoscy.Services.Api;
using System.IO;
using System.Threading.Tasks;
using Hoscy.Services.Speech.Utilities;

namespace Hoscy.Services.Speech.Recognizers
{
    public class RecognizerApi : RecognizerBase
    {
        new public static RecognizerPerms Perms => new()
        {
            UsesMicrophone = true
        };

        public override bool IsListening => _isListening;
        private bool _isListening = false;

        private readonly WaveIn _microphone = new()
        {
            DeviceNumber = Devices.GetMicrophoneIndex(Config.Speech.MicId),
            WaveFormat = new(sampleRate: 16000, 1)
        };
        protected MemoryStream? _stream = new();
        private readonly ApiClient _client = new();

        #region Starting / Stopping
        protected override bool StartInternal()
        {
            var preset = Config.Api.GetPreset(Config.Api.RecognitionPreset);
            if (preset == null)
            {
                Logger.Warning("Attempted to use a non existant preset");
                return false;
            }

            if (!_client.LoadPreset(preset))
                return false;

            _microphone.DataAvailable += OnDataAvailable;
            _microphone.RecordingStopped += OnRecordingStopped;
            return true;
        }

        protected override bool SetListeningInternal(bool enabled)
        {
            if (_isListening == enabled)
                return false;

            _isListening = enabled;

            if (enabled)
                _microphone.StartRecording();
            else
                _microphone.StopRecording();

            Textbox.EnableTyping(enabled);
            Logger.PInfo("Microphone status changed to " + enabled);
            return true;
        }

        protected override void StopInternal()
        {
            _client.Clear();
            _microphone.Dispose();
            _stream?.Dispose();
            _stream = null;
        }
        #endregion

        #region Events
        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (_stream == null)
                return;

            Textbox.EnableTyping(false);
            if (_client == null)
            {
                _stream.SetLength(0);
                return;
            }

            _stream.Position = 0;
            Task.Run(() => RequestRecognition(_stream.GetBuffer())).ConfigureAwait(false);

            _stream.SetLength(0);
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_stream == null)
                return;

            _stream.Write(e.Buffer, 0, e.BytesRecorded);

            if (_stream.Position >= _microphone.WaveFormat.AverageBytesPerSecond * Config.Api.RecognitionMaxRecordingTime)
                SetListening(false);
        }

        private async Task RequestRecognition(byte[] audioData)
        {
            if (audioData.Length == 0)
                return;

            var result = await _client.SendBytes(audioData);

            var message = Denoise(result);
            if (string.IsNullOrWhiteSpace(message))
                return;

            ProcessMessage(message);
        }
        #endregion
    }
}
