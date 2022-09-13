using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace OscMultitool.Services.Speech
{
    public static class Devices
    {
        #region Mics
        public static IReadOnlyList<WaveInCapabilities> Microphones { get; private set; } = GetMicrophones();
        private static IReadOnlyList<WaveInCapabilities> GetMicrophones()
        {
            Logger.Info("Getting list of Microphones", "Devices");
            var list = new List<WaveInCapabilities>();
            for (int i = 0; i < WaveIn.DeviceCount; i++)
                list.Add(WaveIn.GetCapabilities(i));

            return list;
        }
        public static int GetMicrophoneIndex(string guid)
        {
            for (int i = 0; i < Microphones.Count; i++)
            {
                if (Microphones[i].ProductName == guid)
                    return i;
            }
            return -1;
        }
        #endregion

        #region Speakers
        public static IReadOnlyList<WaveOutCapabilities> Speakers { get; private set; } = GetSpeakers();
        private static IReadOnlyList<WaveOutCapabilities> GetSpeakers()
        {
            Logger.Info("Getting list of Speakers", "Devices");
            var speakers = new List<WaveOutCapabilities>();
            for (int i = 0; i < WaveOut.DeviceCount; i++)
                speakers.Add(WaveOut.GetCapabilities(i));

            return speakers;
        }

        public static int GetSpeakerIndex(string guid)
        {
            for (int i = 0; i < Speakers.Count; i++)
            {
                if (Speakers[i].ProductName == guid)
                    return i;
            }

            return -1;
        }
        #endregion
    }
}
