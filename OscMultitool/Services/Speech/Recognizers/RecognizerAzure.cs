using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.CoreAudioApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hoscy.Services.Speech.Recognizers
{
    public class RecognizerAzure : RecognizerBase
    {
        new public static RecognizerPerms Perms => new()
        {
            Description = "Remote recognition using Azure-API",
            UsesMicrophone = true
        };

        public override bool IsListening => _isListening;
        private bool _isListening = false;

        private SpeechRecognizer? _rec;
        private TaskCompletionSource<int>? recognitionCompletionSource;


        #region Starting / Stopping
        protected override bool StartInternal()
        {
            _rec = TryCreateRecognizer();
            return _rec != null;
        }

        #region Setup
        private SpeechRecognizer? TryCreateRecognizer()
        {
            SpeechRecognizer? rec = null;

            try
            {
                var audioConfig = AudioConfig.FromMicrophoneInput(GetMicId());
                var speechConfig = SpeechConfig.FromSubscription(Config.Api.AzureKey, Config.Api.AzureRegion);
                speechConfig.SetProfanity(ProfanityOption.Raw);

                if (!string.IsNullOrWhiteSpace(Config.Api.AzureCustomEndpoint))
                    speechConfig.EndpointId = Config.Api.AzureCustomEndpoint;

                if (!string.IsNullOrWhiteSpace(Config.Api.AzureLanguage))
                    speechConfig.SpeechRecognitionLanguage = Config.Api.AzureLanguage;

                rec = new(speechConfig, audioConfig);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return rec;
            }

            rec.Recognized += OnRecognized;
            rec.Canceled += OnCanceled;
            rec.SessionStopped += OnStopped;
            rec.SessionStarted += OnStarted;

            return rec;
        }

        private static string GetMicId()
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                foreach (var mic in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                {
                    if (mic.FriendlyName.Contains(Config.Speech.MicId))
                        return mic.ID;
                }
            }

            return string.Empty;
        }
        #endregion

        protected override void StopInternal()
        {
            recognitionCompletionSource?.TrySetResult(0);

            while (recognitionCompletionSource != null)
                Thread.Sleep(10);

            _rec?.Dispose();
            _rec = null;
        }

        protected override bool SetListeningInternal(bool enabled)
        {
            if (_isListening == enabled)
                return true;

            _isListening = enabled;

            if (_isListening)
                Task.Run(StartRecognizing).ConfigureAwait(false);
            else
                recognitionCompletionSource?.SetResult(0);

            return true;
        }

        private async Task StartRecognizing()
        {
            if (_rec == null)
                return;

            recognitionCompletionSource = new();
            await _rec.StartContinuousRecognitionAsync();
            await recognitionCompletionSource.Task.ConfigureAwait(false);
            await _rec.StopContinuousRecognitionAsync();
            recognitionCompletionSource = null;
        }
        #endregion

        #region Events
        private void OnRecognized(object? sender, SpeechRecognitionEventArgs e)
        {
            Logger.Log("Got Message: " + e.Result.Text);

            var message = Denoise(e.Result.Text);
            if (string.IsNullOrWhiteSpace(message))
                return;

            ProcessMessage(message);
        }

        private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            if (e.ErrorCode != CancellationErrorCode.NoError)
                Logger.Warning($"Recognition was cancelled (Reason: {CancellationReason.Error}, Code: {e.ErrorCode}, Details: {e.ErrorDetails})");

            if (e.ErrorCode != CancellationErrorCode.ConnectionFailure)
            {
                SetListening(false);
                return;
            }

            Logger.PInfo("Attempting to restart recognizer as it failed connecting");
            StopInternal();
            _rec = TryCreateRecognizer();
        }

        private void OnStopped(object? sender, SessionEventArgs e)
            => Logger.Info("Recognition was stopped");
        private void OnStarted(object? sender, SessionEventArgs e)
            => Logger.Info("Recognition was started");
        #endregion
    }
}