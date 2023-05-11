using NAudio.Wave;
using System;
using System.Linq;

namespace Hoscy.Services.Speech.Utilities
{
    /// <summary>
    /// Proxy for mic that allows continous recording
    /// </summary>
    internal class WaveInProxy //todo: [REFACTOR] fields
    {
        private readonly WaveIn _microphone;
        private static readonly WaveInEventArgs _emptyWaveInData = new(new byte[3200].Select(x => x = 0).ToArray(), 3200);
        internal bool IsRunning { get; private set; } = false;
        internal bool IsListening { get; private set; } = false;

        internal WaveInProxy()
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
        //todo: [REFACTOR] cleanup
        internal bool Unmute()
            => SetMuteStatus(true);
        internal bool Mute()
            => SetMuteStatus(false);
        internal bool SetMuteStatus(bool enabled)
        {
            if (!IsRunning || enabled == IsListening)
                return false;

            IsListening = enabled;
            Logger.PInfo("Microphone listening set to " + IsListening);
            return true;
        }

        internal bool Start()
            => SetMicStatus(true);
        internal bool Stop()
            => SetMicStatus(false);
        internal bool SetMicStatus(bool enabled)
        {
            if (enabled == IsRunning)
                return false;

            if (enabled)
                _microphone.StartRecording();
            else
                _microphone.StopRecording();

            IsRunning = enabled;
            Logger.PInfo("Microphone running set to " + IsRunning);
            return true;
        }
        #endregion

        #region Events
        internal event EventHandler<WaveInEventArgs> DataAvailable = delegate { };

        internal event EventHandler<StoppedEventArgs> RecordingStopped = delegate { };

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
        internal void Dispose()
        {
            _microphone.Dispose();
        }
        #endregion
    }
}
