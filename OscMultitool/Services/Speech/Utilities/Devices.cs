using NAudio.Wave;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using Whisper;

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

        /// <summary>
        /// Returns the microphone list index for a given GUID
        /// </summary>
        /// <param name="guid">GUID of microphone</param>
        /// <returns>On match => Index, No match => 0, Empty list => -1</returns>
        internal static int GetMicrophoneIndex(string guid)
        {
            for (int i = 0; i < Microphones.Count; i++)
            {
                if (Microphones[i].ProductName == guid)
                    return i;
            }

            if (Microphones.Count == 0)
            {
                Logger.Error("No microphones available in list, this will cause some major issues", false);
                return -1;
            }

            Logger.Warning("No matching microphone found, defaulting to first in list...");
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

        /// <summary>
        /// Returns the speaker list index for a given GUID
        /// </summary>
        /// <param name="guid">GUID of speaker</param>
        /// <returns>On match => Index, No match => 0, Empty list => -1</returns>
        internal static int GetSpeakerIndex(string guid)
        {
            for (int i = 0; i < Speakers.Count; i++)
            {
                if (Speakers[i].ProductName == guid)
                    return i;
            }

            if (Speakers.Count == 0)
            {
                Logger.Error("No speakers available in list, this might cause some issues", false);
                return -1;
            }

            Logger.Warning("No matching speaker found, defaulting to first in list...");
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

        /// <summary>
        /// Returns the listener list index for a given id
        /// </summary>
        /// <param name="id">ID of listener</param>
        /// <returns>On match => Index, No match => 0, Empty list => -1</returns>
        internal static int GetWindowsListenerIndex(string id)
        {
            for (int i = 0; i < WindowsRecognizers.Count; i++)
            {
                if (WindowsRecognizers[i].Id == id)
                    return i;
            }

            if (WindowsRecognizers.Count == 0)
            {
                Logger.Error("No windows recognizers available in list, this might cause some issues", false);
                return -1;
            }

            Logger.Warning("No matching windows recognizer found, defaulting to first in list...");
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

        /// <summary>
        /// Returns the voice list index for a given ID
        /// </summary>
        /// <param name="id">ID of voice</param>
        /// <returns>On match => Index, No match => 0, Empty list => -1</returns>
        internal static int GetWindowsVoiceIndex(string id)
        {
            for (int i = 0; i < WindowsVoices.Count; i++)
            {
                if (WindowsVoices[i].Id == id)
                    return i;
            }

            if (WindowsVoices.Count == 0)
            {
                Logger.Error("No windows voices available in list, this might cause some issues", false);
                return -1;
            }

            Logger.Warning("No matching windows voice found, defaulting to first in list...");
            return 0;
        }
        #endregion

        #region GPUs
        internal static IReadOnlyList<string> GraphicsAdapters { get; private set; } = GetGraphicsAdapters();
        private static IReadOnlyList<string> GetGraphicsAdapters()
            => new List<string>(Library.listGraphicAdapters());

        /// <summary>
        /// Returns the adapter list index for a given ID
        /// </summary>
        /// <param name="id">ID of adapter</param>
        /// <returns>On match => Index, No match => 0, Empty list => -1</returns>
        internal static int GetGraphicsAdapterIndex(string id)
        {
            if (GraphicsAdapters.Count == 0)
            {
                Logger.Error("No graphics adapters available in list, this might cause some issues", false);
                return -1;
            }

            for(int i = 0; i < GraphicsAdapters.Count; i++)
            {
                if (GraphicsAdapters[i] == id)
                    return i;
            }

            Logger.Warning("No matching graphics adapter found, defaulting to first in list...");
            return 0;
        }
        #endregion
    }
}
