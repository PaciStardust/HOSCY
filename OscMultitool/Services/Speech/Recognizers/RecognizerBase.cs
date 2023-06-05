using Hoscy.Services.OscControl;
using Hoscy.Services.Speech.Recognizers;
using System;

namespace Hoscy.Services.Speech
{
    internal abstract class RecognizerBase
    {
        internal static RecognizerPerms Perms => new();
        internal bool IsRunning { get; private set; } = false;
        internal abstract bool IsListening { get; }

        /// <summary>
        /// Init
        /// </summary>
        internal RecognizerBase() { }

        #region Starting / Stopping
        /// <summary>
        /// Override for internal start procedures
        /// </summary>
        /// <returns>Success</returns>
        abstract protected bool StartInternal();

        /// <summary>
        /// Start the listener
        /// </summary>
        /// <returns>Success</returns>
        internal bool Start()
        {
            if (IsRunning)
            {
                Logger.Warning("Tried to start recognizer while it is already running");
                return true;
            }

            IsRunning = StartInternal();

            if (!IsRunning)
                return false;

            if (Config.Speech.StartUnmuted)
                SetListening(true);

            return true;
        }

        /// <summary>
        /// Override for internal stop procedures, please dispose of anything, restarting is not intended
        /// </summary>
        abstract protected void StopInternal();

        /// <summary>
        /// Stops the listener
        /// </summary>
        internal void Stop()
        {
            if (!IsRunning)
            {
                Logger.Warning("Tried to stop recognizer while it is not running");
                return;
            }

            SetListening(false);
            StopInternal();
            IsRunning = false;
        }
        #endregion

        #region Mic
        /// <summary>
        /// Enables or disables listening
        /// </summary>
        /// <param name="enabled">Mode</param>
        internal bool SetListening(bool enabled)
        {
            if (!IsRunning || IsListening == enabled)
                return false;

            bool result = SetListeningInternal(enabled);
            if (!result)
                Logger.Warning("Failed to change mic status");
            else
            {
                //todo: [TEST] Does this stop the talking indicator on mute? Is this even needed?
                if (!enabled)
                    HandleSpeechChanged(false);

                var packet = new OscPacket(Config.Osc.AddressListeningIndicator, enabled);//Ingame listening indicator
                if (!packet.IsValid)
                    Logger.Warning("Unable to send data to ingame listening indicator, packet is invalid");
                else
                    Osc.Send(packet);
            }

            return result;
        }

        /// <summary>
        /// Enables or disables listening, internal procedures
        /// </summary>
        /// <param name="enabled">Mode</param>
        protected abstract bool SetListeningInternal(bool enabled);
        #endregion

        #region Events
        internal event EventHandler<string> SpeechRecognized = delegate { };
        protected void HandleSpeechRecognized(string text)
            => SpeechRecognized.Invoke(null, text);

        internal event EventHandler<bool> SpeechChanged = delegate { };
        protected void HandleSpeechChanged(bool mode)
        {
            if (mode && (!IsListening || (!Config.Speech.UseTextbox && !Config.Textbox.UseIndicatorWithoutBox)))
                return;

            SpeechChanged.Invoke(null, mode);
        }
        #endregion
    }
}
