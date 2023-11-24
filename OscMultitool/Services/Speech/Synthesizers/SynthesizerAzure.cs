using Microsoft.CognitiveServices.Speech;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Hoscy.Services.Speech.Synthesizers
{
    internal class SynthesizerAzure : SynthesizerBase
    {
        #region Functionality
        private readonly SpeechSynthesizer? _synth;

        internal SynthesizerAzure(MemoryStream stream) : base(stream)
        {
            SpeechSynthesizer? synth = null;

            Logger.PInfo("Performing load of Azure Synthesizer");
            try
            {
                var speechCfg = SpeechConfig.FromSubscription(Config.Api.AzureKey, Config.Api.AzureRegion);

                speechCfg.SetProfanity(ProfanityOption.Raw);

                if (!string.IsNullOrWhiteSpace(Config.Api.AzureCustomEndpointSpeech))
                    speechCfg.EndpointId = Config.Api.AzureCustomEndpointSpeech;

                var currentVoiceIndex = Config.Api.GetTtsVoiceIndex(Config.Api.AzureTtsVoiceCurrent);
                if (currentVoiceIndex != -1)
                {
                    var voice = Config.Api.AzureTtsVoices[currentVoiceIndex];
                    if (!string.IsNullOrWhiteSpace(voice.Voice))
                        speechCfg.SpeechSynthesisVoiceName = voice.Voice;
                    if (!string.IsNullOrWhiteSpace(voice.Language))
                        speechCfg.SpeechSynthesisLanguage = voice.Language;
                }

                speechCfg.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm);

                synth = new SpeechSynthesizer(speechCfg, null);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unable to connect to Azure Cognitive Services, have you used the correct credentials?");
            }

            _synth = synth;
        }
        #endregion

        #region Overrides
        internal override bool IsAsync => true;

        internal override bool Speak(string text)
            => throw new NotImplementedException();

        internal override async Task<bool> SpeakAsync(string text)
        {
            if (_synth == null || _stream == null)
                return false;

            var startTime = DateTime.Now;
            var result = await _synth.SpeakTextAsync(text);

            switch (result.Reason)
            {
                case ResultReason.SynthesizingAudioCompleted:
                    Logger.Log($"Received TTS audio for \"{text}\" ({(DateTime.Now - startTime).TotalMilliseconds}ms) => {result.AudioDuration:mm\\:ss\\.fff}");
                    _stream.Write(result.AudioData);
                    return true;

                case ResultReason.Canceled:
                    var e = SpeechSynthesisCancellationDetails.FromResult(result);

                    if (e.Reason == CancellationReason.Error)
                        Logger.Warning($"TTS \"{text}\" was cancelled (Reason: {CancellationReason.Error}, Code: {e.ErrorCode}, Details: {e.ErrorDetails})");
                    else
                        Logger.Log($"TTS for \"{text}\" was cancelled (Reason: {CancellationReason.Error})");
                    return false;

                default:
                    return false;
            }
        }
        #endregion
    }
}
