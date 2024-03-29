﻿using Hoscy.Services.OscControl;
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
                if (!enabled)
                    HandleSpeechActivityUpdated(false);

                var packet = new OscPacket(Config.Osc.AddressListeningIndicator, enabled); //Ingame listening indicator
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
        /// <summary>
        /// Event gets triggered whenever speech is recognized, is preprocessed a little depending on recognizer
        /// </summary>
        internal event EventHandler<string> SpeechRecognized = delegate { };
        protected void HandleSpeechRecognized(string text)
            => SpeechRecognized.Invoke(null, text);

        /// <summary>
        /// Event gets triggered each time speech activity is updated, might happen multiple times a second using the same value, depends on recognizer
        /// </summary>
        internal event EventHandler<bool> SpeechActivityUpdated = delegate { };
        protected void HandleSpeechActivityUpdated(bool mode)
        {
            if (mode && (!IsListening || (!Config.Speech.UseTextbox && !Config.Textbox.UseIndicatorWithoutBox)))
                return;

            SpeechActivityUpdated.Invoke(null, mode);
        }
        #endregion
    }
}
