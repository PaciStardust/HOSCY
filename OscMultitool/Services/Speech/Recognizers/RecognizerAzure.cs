using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.CoreAudioApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hoscy.Services.Speech.Recognizers
{
    internal class RecognizerAzure : RecognizerBase
    {
        new internal static RecognizerPerms Perms => new()
        {
            Description = "Remote recognition using Azure-API",
            UsesMicrophone = true,
            Type = RecognizerType.Azure
        };

        internal override bool IsListening => _isListening;
        private bool _isListening = false;

        private SpeechRecognizer? _rec;
        private TaskCompletionSource<int>? recognitionCompletionSource;

        #region Setup
        private SpeechRecognizer? TryCreateRecognizer()
        {
            SpeechRecognizer? rec = null;

            try
            {
                bool multLang = Config.Api.AzureRecognitionLanguages.Count > 1;

                var audioConfig = AudioConfig.FromMicrophoneInput(GetMicId());
                var speechConfig = multLang //Config has to be adapted if using multiple languages
                    ? SpeechConfig.FromEndpoint(new($"wss://{Config.Api.AzureRegion}.stt.speech.microsoft.com/speech/universal/v2"), Config.Api.AzureKey)
                    : SpeechConfig.FromSubscription(Config.Api.AzureKey, Config.Api.AzureRegion);
                speechConfig.SetProfanity(ProfanityOption.Raw);

                if (!string.IsNullOrWhiteSpace(Config.Api.AzureCustomEndpointRecognition))
                    speechConfig.EndpointId = Config.Api.AzureCustomEndpointRecognition;

                if (multLang) //this looks scuffed but is done as I think its quicker in api terms
                {
                    speechConfig.SetProperty(PropertyId.SpeechServiceConnection_LanguageIdMode, "Continuous");
                    var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(Config.Api.AzureRecognitionLanguages.ToArray());
                    rec = new(speechConfig, autoDetectSourceLanguageConfig, audioConfig);
                }
                else
                {
                    if (Config.Api.AzureRecognitionLanguages.Count == 1)
                        speechConfig.SpeechRecognitionLanguage = Config.Api.AzureRecognitionLanguages[0];
                    rec = new(speechConfig, audioConfig);
                }

                if (Config.Api.AzurePhrases.Count != 0)
                {
                    var phraseList = PhraseListGrammar.FromRecognizer(rec);
                    foreach (var phrase in Config.Api.AzurePhrases)
                        phraseList.AddPhrase(phrase);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unable to connect to Azure Cognitive Services, have you used the correct credentials?");
                return rec;
            }

            rec.Recognized += OnRecognized;
            rec.Canceled += OnCanceled;
            rec.SessionStopped += OnStopped;
            rec.SessionStarted += OnStarted;

            return rec;
        }

        private static string GetMicId() //todo: [REFACTOR] Implement default
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

        #region Starting / Stopping
        protected override bool StartInternal()
        {
            _rec = TryCreateRecognizer();
            return _rec != null;
        }

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
                StartRecognizing().RunWithoutAwait();
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
            var result = e.Result.Text;
            if (string.IsNullOrWhiteSpace(result))
                return;

            Logger.Log("Got Message: " + result);
            HandleSpeechRecognized(result);
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