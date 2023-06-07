using Hoscy.Services.Speech.Utilities;
using System;
using System.IO;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace Hoscy.Services.Speech.Synthesizers
{
    internal class SynthesizerWindows : SynthesizerBase
    {
        #region Functionality
        private readonly SpeechSynthesizer? _synth;

        internal SynthesizerWindows(MemoryStream stream) : base(stream)
        {
            var synth = new SpeechSynthesizer();

            var formatInfo = new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
            synth.SetOutputToAudioStream(_stream, formatInfo);

            if (Devices.WindowsVoices.Count == 0)
            {
                Logger.Warning("Could not find a voice to use for synthesizer");
                return;
            }

            var voiceIndex = Math.Max(Devices.GetWindowsVoiceIndex(Config.Speech.TtsId), 0);
            synth.SelectVoice(Devices.WindowsVoices[voiceIndex].Name);

            _synth = synth;
        }
        #endregion

        #region Overrides
        internal override bool IsAsync => false;

        internal override bool Speak(string text)
        {
            if (_synth == null || _stream == null)
                return false;

            _synth.Speak(text);
            return true;
        }

        internal override Task<bool> SpeakAsync(string text)
            => throw new NotImplementedException();
        #endregion
    }
}
