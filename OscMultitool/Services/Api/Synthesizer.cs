using Microsoft.CognitiveServices.Speech;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Hoscy.Services.Api
{
    public static class Synthesizer
    {
        private static SpeechSynthesizer? _synth;

        static Synthesizer()
        {
            ReloadClient();
        }

        /// <summary>
        /// Reloads the Synthesizer client
        /// </summary>
        public static void ReloadClient()
        {
            if (_synth != null)
            {
                _synth.Dispose();
                _synth = null;
            }
            SpeechSynthesizer? synth = null;

            Logger.PInfo("Performing reload of Azure Synthesizer");
            try
            {
                var speechCfg = SpeechConfig.FromSubscription(Config.Api.AzureKey, Config.Api.AzureRegion);

                speechCfg.SetProfanity(ProfanityOption.Raw);

                if (!string.IsNullOrWhiteSpace(Config.Api.AzureCustomEndpointSpeech))
                    speechCfg.EndpointId = Config.Api.AzureCustomEndpointSpeech;

                if (!string.IsNullOrWhiteSpace(Config.Api.AzureVoice))
                    speechCfg.SpeechSynthesisVoiceName = Config.Api.AzureVoice;

                if (!string.IsNullOrWhiteSpace(Config.Api.AzureSpeechLanguage))
                    speechCfg.SpeechSynthesisLanguage = Config.Api.AzureSpeechLanguage;

                speechCfg.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm);

                synth = new SpeechSynthesizer(speechCfg, null);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unable to connect to Azure Cognitive Services, have you used the correct credentials?");
            }

            _synth = synth;
        }

        /// <summary>
        /// Writes audio data into memorystream
        /// </summary>
        /// <param name="text">Text to synthesize</param>
        /// <param name="ms">Memorystream</param>
        /// <returns>Success</returns>
        public static async Task<bool> SpeakAsync(string text, MemoryStream ms)
        {
            if (_synth == null)
                return false;

            var startTime = DateTime.Now;
            var result = await _synth.SpeakTextAsync(text);

            switch (result.Reason)
            {
                case ResultReason.SynthesizingAudioCompleted:
                    Logger.Log($"Received TTS audio for \"{text}\" ({(DateTime.Now - startTime).TotalMilliseconds}ms) => {result.AudioDuration:mm\\:ss\\.fff}");
                    ms.Write(result.AudioData);
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
    }
}
