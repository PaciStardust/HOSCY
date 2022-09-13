using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OscMultitool.Services.Speech
{
    /// <summary>
    /// Proxy for mic that allows continous recording
    /// </summary>
    public class WaveInProxy
    {
        private readonly WaveIn _microphone;
        private static readonly WaveInEventArgs _emptyWaveInData = new(new byte[3200].Select(x => x = 0).ToArray(), 3200);
        public bool IsRunning { get; private set; } = false;
        public bool IsListening { get; private set; } = false;

        public WaveInProxy()
        {
            int micId = Devices.GetMicrophoneIndex(Config.Speech.MicId);
            _microphone = new WaveIn()
            {
                DeviceNumber = micId,
                WaveFormat = new(16000, 1)
            };
            _microphone.RecordingStopped += HandleRecordingStopped;
            _microphone.DataAvailable += HandleDataAvailable;
        }

        #region Stopping / Starting
        public bool Unmute()
            => SetMuteStatus(true);
        public bool Mute()
            => SetMuteStatus(false);
        public bool SetMuteStatus(bool enabled)
        {
            if (!IsRunning || enabled == IsListening)
                return false;

            IsListening = enabled;
            Logger.PInfo("Microphone listening set to " + IsListening, "WaveInProxy");
            return true;
        }

        public bool Start()
            => SetMicStatus(true);
        public bool Stop()
            => SetMicStatus(false);
        public bool SetMicStatus(bool enabled)
        {
            if (enabled == IsRunning)
                return false;

            if (enabled)
                _microphone.StartRecording();
            else
                _microphone.StopRecording();

            IsRunning = enabled;
            Logger.PInfo("Microphone running set to " + IsRunning, "WaveInProxy");
            return true;
        }
        #endregion

        #region Events
        public event EventHandler<WaveInEventArgs> DataAvailable = delegate { };

        public event EventHandler<StoppedEventArgs> RecordingStopped = delegate { };

        private void HandleDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!IsRunning)
                return;

            if (IsListening)
                DataAvailable.Invoke(this, e);
            else
                DataAvailable.Invoke(this, _emptyWaveInData);
        }

        private void HandleRecordingStopped(object? sender, StoppedEventArgs e)
            => RecordingStopped.Invoke(this, e);

        #endregion

        #region Extras
        public void Dispose()
        {
            _microphone.Dispose();
        }
        #endregion
    }
}
