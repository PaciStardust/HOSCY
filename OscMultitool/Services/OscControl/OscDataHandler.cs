using Hoscy.Services.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Hoscy.Services.OscControl
{
    internal static class OscDataHandler
    {
        #region Basic Handling
        /// <summary>
        /// Internal parsing for osc in case it triggers a command
        /// </summary>
        /// <param name="address">Target address of packet</param>
        /// <param name="arguments">Arguments of packet</param>
        internal static void Handle(string address, object[] arguments)
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
            if (Config.Speech.MuteOnVrcMute && address == Config.Osc.AddressGameMute)
                Recognition.SetListening(!value);

            if (address == Config.Osc.AddressGameAfk)
                SetAfkTimer(value);

            if (!value) //Options below will be triggered with an osc button so this avoids triggering twice
                return;

            //Checking counters
            CheckForCounters(address);

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

        #region Functionality
        /// <summary>
        /// Checks for counter increases
        /// </summary>
        /// <param name="address">Osc Address</param>
        private static void CheckForCounters(string address)
        {
            foreach (var counter in Config.Osc.Counters)
            {
                if (counter.Parameter != address)
                    continue;

                counter.Count++;
                Logger.Debug($"Counter \"{counter.Count}\" ({counter.Parameter}) increased to {counter.Count}");
                Textbox.Notify(counter.ToString(), NotificationType.Counter);
                break;
            }
        }

        private static Timer? _afkTimer;
        private static DateTime _afkStarted = DateTime.Now;
        private static void SetAfkTimer(bool mode)
        {
            if (Config.Osc.ShowAfkDuration && mode && _afkTimer == null)
            {
                Textbox.Notify("User now AFK", NotificationType.Afk);
                _afkStarted = DateTime.Now;

                _afkTimer = new(Config.Osc.AfkDuration);
                _afkTimer.Elapsed += AfkTimerElapsed;
                _afkTimer.Start();

                Logger.Log("AFK Timer started");
                return;
            }
            else if (!mode && _afkTimer != null)
            {
                Textbox.Notify("User no longer AFK", NotificationType.Afk);
                _afkTimer.Stop();
                _afkTimer.Dispose();
                _afkTimer = null;
                Logger.Log("AFK Timer stopped");
            }
        }

        private static void AfkTimerElapsed(object? sender, ElapsedEventArgs e)
            => Textbox.Notify("User AFK since " + (e.SignalTime.AddMilliseconds(500) - _afkStarted).ToString(@"hh\:mm\:ss"), NotificationType.Afk);
        #endregion
    }
}
