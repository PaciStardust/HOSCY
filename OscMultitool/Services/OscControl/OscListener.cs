using Hoscy;
using Hoscy.Services.Speech;
using SharpOSC;
using System;
using System.Collections.Generic;

namespace Hoscy.OscControl
{
    public class OscListener
    {
        private readonly IReadOnlyList<OscRoutingFilter> _filters;
        private UDPListener? _listener;
        private readonly int _port;
        public int Port => _port;

        public OscListener(int port, List<OscRoutingFilter> filters)
        {
            _filters = filters;
            _port = port;
        }

        #region Starting, Ending
        /// <summary>
        /// Starts the OSC Listener
        /// </summary>
        /// <returns>Running status</returns>
        public bool Start()
        {
            if (_listener != null)
                return true;

            try
            {
                _listener = new UDPListener(_port, Callback);
                Logger.PInfo($"OSC-Listener is now listening on port {_port}");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Callback for parsing incoming osc packets
        /// </summary>
        /// <param name="packet">Incomping packet</param>
        private void Callback(SharpOSC.OscPacket packet)
        {
            var message = (OscMessage)packet;

            if (message == null)
            {
                Logger.Debug("Received an empty packet, skipping");
                return;
            }

            var argsInfo = new List<string>();
            foreach (var arg in message.Arguments)
                argsInfo.Add($"{arg.GetType().Name}({arg})" ?? "???");

            Logger.Debug($"Packet has been received on port {_port} ({message.Address}) => {string.Join(", ", argsInfo)}");
            HandleData(message.Address, message.Arguments.ToArray());
        }

        /// <summary>
        /// Stops the listener, it should always be running but can be stopped in case it needs to be restarted
        /// </summary>
        public void Stop()
        {
            _listener?.Close();
            //_listener?.Dispose(); This somehow crashes the program
            _listener = null;
            Logger.PInfo($"Stopped listener on port " + _port);
        }
        #endregion

        #region Data Handling
        /// <summary>
        /// Handles the data by sending it to all matching filters and checks for internal uses
        /// </summary>
        /// <param name="address">Target Address of packet</param>
        /// <param name="arguments">Arguments of packet</param>
        private void HandleData(string address, object[] arguments)
        {
            HandleToolData(address, arguments);

            foreach (var filter in _filters)
                if (filter.Matches(address))
                    filter.Send(address, arguments);
        }

        /// <summary>
        /// Internal parsing for osc in case it triggers a command
        /// </summary>
        /// <param name="address">Target address of packet</param>
        /// <param name="arguments">Arguments of packet</param>
        private static void HandleToolData(string address, object[] arguments)
        {
            if (arguments.Length == 0 || arguments[0] == null)
                return;

            Type type = arguments[0].GetType();

            if (type == typeof(bool))
                HandleToolDataBool(address, (bool)arguments[0]);
            else if (type == typeof(string))
                HandleToolDataString(address, (string)arguments[0]);
        }

        /// <summary>
        /// Handles all internal osc commands of type bool
        /// </summary>
        /// <param name="address">Target address of packet</param>
        /// <param name="value">Bool value</param>
        private static void HandleToolDataBool(string address, bool value)
        {
            if (Config.Speech.MuteOnVrcMute && address == "/avatar/parameters/MuteSelf")
                Recognition.SetListening(!value);

            if (!value) //Options below will be triggered with an osc button so this avoids triggering twice
                return;

            if (address == Config.Osc.AddressManualMute)
                Recognition.SetListening(!Recognition.IsRecognizerListening);

            if (address == Config.Osc.AddressManualSkipBox)
                Textbox.Clear();

            if (address == Config.Osc.AddressManualSkipSpeech)
                Synthesizing.Skip();

            if (address == Config.Osc.AddressEnableAutoMute)
            {
                bool newValue = !Config.Speech.MuteOnVrcMute;
                Logger.Info("'Mute on VRC mute' has been changed via OSC => " + newValue);
                Config.Speech.MuteOnVrcMute = !Config.Speech.MuteOnVrcMute;
            }

            if (address == Config.Osc.AddressEnableTextbox)
            {
                bool newValue = !Config.Speech.UseTextbox;
                Logger.Info("'Textbox on Speech' has been changed via OSC => " + newValue);
                Config.Speech.UseTextbox = newValue;
            }

            if (address == Config.Osc.AddressEnableTts)
            {
                bool newValue = !Config.Speech.UseTts;
                Logger.Info("'TTS on Speech' has been changed via OSC => " + newValue);
                Config.Speech.UseTts = !Config.Speech.UseTts;
            }

            if (address == Config.Osc.AddressEnableReplacements)
            {
                bool newValue = !Config.Speech.UseReplacements;
                Logger.Info("'Replacements and Shortcuts for Speech' has been changed via OSC => " + newValue);
                Config.Speech.UseReplacements = !Config.Speech.UseReplacements;
            }
        }

        /// <summary>
        /// Handles all internal osc commands of type string
        /// </summary>
        /// <param name="address">Target address of packet</param>
        /// <param name="value">String value</param>
        private static void HandleToolDataString(string address, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (address == Config.Osc.AddressAddTextbox || address == Config.Osc.AddressAddTts)
            {
                var tProcessor = new TextProcessor()
                {
                    ReplaceCaseInsensitive = true,
                    TriggerReplace = true,
                    TriggerCommands = true,
                    UseTextbox = address == Config.Osc.AddressAddTextbox,
                    UseTts = address == Config.Osc.AddressAddTts,
                    AllowTranslate = Config.Api.TranslationAllowExternal
                };
                tProcessor.Process(value);
            }

            if (address == Config.Osc.AddressAddNotification)
                Textbox.Notify(value, NotificationType.External);
        }
        #endregion
    }
}
