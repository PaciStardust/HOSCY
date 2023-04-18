using Hoscy.Services.Speech.Synthesizers;
using Hoscy.Services.Speech.Utilities;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading;

namespace Hoscy.Services.Speech
{
    internal static class Synthesizing //todo: test
    {
        private static SynthesizerBase _synth;
        private static readonly MemoryStream _stream = new();
        private static readonly WaveOut _waveOut = new();
        private static RawSourceWaveStream? _provider;
        private static string _currentString = string.Empty;
        internal static bool IsRunning => _provider != null;

        static Synthesizing()
        {
            _synth = CreateSynth();

            //Mic Setup
            _provider = GenerateProvider();
            _waveOut.Init(_provider);

            ChangeSpeakers();
            ChangeVolume(); //Turns out the default for this somehow gets saved

            Thread speechThread = new(new ThreadStart(SpeechLoop))
            {
                Name = "Synth",
                IsBackground = true
            };
            speechThread.Start();

            Logger.PInfo("Successfully started synthesizer thread");
        }

        private static SynthesizerBase CreateSynth()
        {
            Logger.PInfo("Attempting to create synthesizer...");

            if (Config.Api.UseAzureTts)
                return new SynthesizerAzure(_stream);

            return new SynthesizerWindows(_stream);
        }

        #region Synth Control
        /// <summary>
        /// Reloads the synthesizer
        /// </summary>
        internal static void ReloadSynth()
            => _synth = CreateSynth();

        /// <summary>
        /// Changers speakers based on config
        /// </summary>
        internal static void ChangeSpeakers()
        {
            var newDeviceNumber = Devices.GetSpeakerIndex(Config.Speech.SpeakerId);

            if (newDeviceNumber == _waveOut.DeviceNumber)
                return;
            
            _waveOut.Stop(); //Only triggers if its not alrady playing
            _waveOut.DeviceNumber = newDeviceNumber;
            _waveOut.Init(_provider); //Redo init
            Logger.PInfo("Changed synthesizer microphone: " + Devices.Speakers[newDeviceNumber].ProductName);
        }

        /// <summary>
        /// Changes the output volume
        /// </summary>
        internal static void ChangeVolume()
        {
            var configVolume = Config.Speech.SpeakerVolume;
            var roundedWaveOutVolume = Math.Ceiling(_waveOut.Volume * 20) / 20f;

            //Floating points messed with this so its strings now
            if (configVolume + string.Empty == roundedWaveOutVolume + string.Empty)
                return;

            _waveOut.Volume = Config.Speech.SpeakerVolume;
            Logger.PInfo("Changed synthesizer volume: " + Config.Speech.SpeakerVolume);
        }
        #endregion

        #region Output
        /// <summary>
        /// Skips the playback by stopping the waveout
        /// </summary>
        internal static void Skip()
        {
            Logger.Log("Skipping current synth audio");
            _waveOut.Stop();
        }

        /// <summary>
        /// Plays and generates the audio for TTS
        /// This works by essentially soft resetting stream and waveOut
        /// </summary>
        /// <param name="input">Sentence to say</param>
        internal static void Say(string input)
        {
            if (!IsRunning || string.IsNullOrWhiteSpace(input))
                return;

            if (input.Length > Config.Speech.MaxLenTtsString)
            {
                if (Config.Speech.SkipLongerMessages)
                {
                    Logger.Log("Skipping a message from synth as it is too long");
                    return;
                }

                input = input[..Config.Speech.MaxLenTtsString];
            }

            //This sets the string for SpeechLoop, see method info why
            _currentString = input;
        }

        /// <summary>
        /// Loop for speech
        /// 
        /// I have made the somewhat strange (in my opinion) choice to make this a loop thread.
        /// If it rust runs on the main thread it causes the application to hang and I cant just start a thread for it
        /// as it could cause it to be started again while previous speech is processing, which would break the stream
        /// I have thought about using a flag for it or a sophomore but the first would kill any speech said while processing
        /// while the other would maybe build up a lot of extra speech to process that would immedeately be cancelled by the next
        /// </summary>
        private async static void SpeechLoop()
        {
            while (App.Running)
            {
                if (_currentString.Length == 0)
                {
                    Thread.Sleep(10);
                    continue;
                }

                ResetStream();
                _waveOut.Stop(); //Might be redundant
                _provider = GenerateProvider(); //We reset the provider for each clip as it appears to cause issues otherwise
                Logger.Log("Creating synth audio from: " + _currentString);

                var speakSuccess = _synth.IsAsync
                    ? await _synth.SpeakAsync(_currentString)
                    : _synth.Speak(_currentString);

                if (!speakSuccess)
                    continue;

                Logger.Info("Playing synth audio: " + _currentString);
                _currentString = string.Empty;
                _stream.Position = 0;
                _waveOut.Play();
            }
        }

        /// <summary>
        /// Utility for resetting the stream
        /// </summary>
        private static void ResetStream()
        {
            _stream.SetLength(0);
            _stream.Capacity = 0;
        }

        /// <summary>
        /// Generates a provider
        /// </summary>
        private static RawSourceWaveStream GenerateProvider()
            => new (_stream, new(16000, 16, 1));
        #endregion
    }
}
