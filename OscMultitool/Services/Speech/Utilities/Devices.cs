using NAudio.Wave;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace Hoscy.Services.Speech.Utilities
{
    internal static class Devices
    {
        internal static void ForceReload()
        {
            Logger.PInfo("Enforcing reload of Devices");
            Microphones = GetMicrophones();
            Speakers = GetSpeakers();
            WindowsRecognizers = GetWindowsRecognizers();
            WindowsVoices = GetWindowsVoices();
        }

        #region Mics
        internal static IReadOnlyList<WaveInCapabilities> Microphones { get; private set; } = GetMicrophones();
        private static IReadOnlyList<WaveInCapabilities> GetMicrophones()
        {
            Logger.Info("Getting list of Microphones");
            var list = new List<WaveInCapabilities>();
            for (int i = 0; i < WaveIn.DeviceCount; i++)
                list.Add(WaveIn.GetCapabilities(i));

            return list;
        }
        internal static int GetMicrophoneIndex(string guid)
        {
            for (int i = 0; i < Microphones.Count; i++)
            {
                if (Microphones[i].ProductName == guid)
                    return i;
            }

            if (Microphones.Count == 0)
                return -1;
            else
                return 0;
        }
        #endregion

        #region Speakers
        internal static IReadOnlyList<WaveOutCapabilities> Speakers { get; private set; } = GetSpeakers();
        private static IReadOnlyList<WaveOutCapabilities> GetSpeakers()
        {
            Logger.Info("Getting list of Speakers");
            var speakers = new List<WaveOutCapabilities>();
            for (int i = 0; i < WaveOut.DeviceCount; i++)
                speakers.Add(WaveOut.GetCapabilities(i));

            return speakers;
        }

        internal static int GetSpeakerIndex(string guid)
        {
            for (int i = 0; i < Speakers.Count; i++)
            {
                if (Speakers[i].ProductName == guid)
                    return i;
            }

            if (Speakers.Count == 0)
                return -1;
            else
                return 0;
        }
        #endregion

        #region WinListeners
        internal static IReadOnlyList<RecognizerInfo> WindowsRecognizers { get; private set; } = GetWindowsRecognizers();
        private static IReadOnlyList<RecognizerInfo> GetWindowsRecognizers()
        {
            Logger.Info("Getting installed Speech Recognizers");
            return SpeechRecognitionEngine.InstalledRecognizers();
        }

        internal static int GetWindowsListenerIndex(string id)
        {
            for (int i = 0; i < WindowsRecognizers.Count; i++)
            {
                if (WindowsRecognizers[i].Id == id)
                    return i;
            }

            if (WindowsRecognizers.Count == 0)
                return -1;
            else
                return 0;
        }
        #endregion

        #region Windows Voices
        internal static IReadOnlyList<VoiceInfo> WindowsVoices { get; private set; } = GetWindowsVoices();
        private static IReadOnlyList<VoiceInfo> GetWindowsVoices()
        {
            Logger.Info("Getting installed Windows Voices");
            using var _synth = new SpeechSynthesizer();
            return _synth.GetInstalledVoices()
                .Where(x => x.Enabled)
                .Select(x => x.VoiceInfo)
                .ToList();
        }

        internal static int GetWindowsVoiceIndex(string id)
        {
            for (int i = 0; i < WindowsVoices.Count; i++)
            {
                if (WindowsVoices[i].Id == id)
                    return i;
            }

            if (WindowsVoices.Count == 0)
                return -1;
            else
                return 0;
        }
        #endregion
    }
}
