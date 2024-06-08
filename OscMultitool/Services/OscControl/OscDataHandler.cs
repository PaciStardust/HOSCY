using Hoscy.Services.Api;
using Hoscy.Services.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <returns>Fully handled?</returns>
        internal static bool Handle(string address, object[] arguments)
        {
            if (arguments.Length == 0 || arguments[0] == null)
                return false;

            Type type = arguments[0].GetType();

            if (type == typeof(bool))
                HandleToolDataBool(address, (bool)arguments[0]);
            else if (type == typeof(string))
                return HandleToolDataString(address, (string)arguments[0]);

            return false;
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

            if (Media.HandleOscMediaCommands(address))
                return;

            else if (address == Config.Osc.AddressManualMute)
                Recognition.SetListening(!Recognition.IsListening);

            else if (address == Config.Osc.AddressManualSkipBox)
                Textbox.Clear();

            else if (address == Config.Osc.AddressManualSkipSpeech)
                Synthesizing.Skip();

            else if (address == Config.Osc.AddressEnableAutoMute)
            {
                bool newValue = !Config.Speech.MuteOnVrcMute;
                Logger.Info("'Mute on VRC mute' has been changed via OSC => " + newValue);
                Config.Speech.MuteOnVrcMute = !Config.Speech.MuteOnVrcMute;
            }

            else if (address == Config.Osc.AddressEnableTextbox)
            {
                bool newValue = !Config.Speech.UseTextbox;
                Logger.Info("'Textbox on Speech' has been changed via OSC => " + newValue);
                Config.Speech.UseTextbox = newValue;
            }

            else if (address == Config.Osc.AddressEnableTts)
            {
                bool newValue = !Config.Speech.UseTts;
                Logger.Info("'TTS on Speech' has been changed via OSC => " + newValue);
                Config.Speech.UseTts = !Config.Speech.UseTts;
            }

            else if (address == Config.Osc.AddressEnableReplacements)
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
        /// <returns>Fully handled?</returns>
        private static bool HandleToolDataString(string address, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var useTextbox = address == Config.Osc.AddressAddTextbox || address == Config.Osc.AddressGameTextbox;
            if (useTextbox || address == Config.Osc.AddressAddTts)
            {
                var tProcessor = new TextProcessor()
                {
                    TriggerReplace = true,
                    TriggerCommands = true,
                    UseTextbox = useTextbox,
                    UseTts = address == Config.Osc.AddressAddTts,
                    AllowTranslate = Config.Api.TranslationAllowExternal
                };
                tProcessor.Process(value, InputSource.Osc);
                return true;
            }

            if (address == Config.Osc.AddressAddNotification)
            {
                Textbox.Notify(value, NotificationType.External);
                return true;
            }  

            return false;
        }
        #endregion

        #region Functionality - Counters
        private static DateTime _counterLastDisplay = DateTime.MinValue;

        /// <summary>
        /// Checks for counter increases
        /// </summary>
        /// <param name="address">Osc Address</param>
        private static void CheckForCounters(string address)
        {
            var now = DateTime.Now;

            foreach (var counter in Config.Osc.Counters)
            {
                if (counter.FullParameter() != address || (now - counter.LastUsed).TotalSeconds < counter.Cooldown)
                    continue;

                counter.Increase();
                Logger.Debug($"Counter \"{counter.Name}\" ({counter.Parameter}) increased to {counter.Count}");

                if (!counter.Enabled) return;

                if (Config.Osc.ShowCounterNotifications && (now - _counterLastDisplay).TotalSeconds > Config.Osc.CounterDisplayCooldown)
                {
                    var counterString = CreateCounterString();
                    if (!string.IsNullOrWhiteSpace(counterString))
                    {
                        _counterLastDisplay = now;
                        Textbox.Notify(counterString, NotificationType.Counter);
                    }
                }

                return;
            }
        }

        /// <summary>
        /// Creates a string of all recently activated counters
        /// </summary>
        /// <returns>The string</returns>
        private static string CreateCounterString()
        {
            var strings = new List<string>();

            var lastUsedEarliest = DateTime.Now.AddSeconds(-Config.Osc.CounterDisplayDuration);
            var validCounters = Config.Osc.Counters
                .Where(x => x.LastUsed >= lastUsedEarliest && x.Enabled)
                .OrderByDescending(x => x.Count); 

            return string.Join(", ", validCounters);
        }
        #endregion

        #region Functionality - AFK Timer
        private static Timer? _afkTimer;
        private static DateTime _afkStarted = DateTime.Now;
        private static uint _afkTimesChecked = 0;

        /// <summary>
        /// Activates or deactivates the AFK timer
        /// </summary>
        /// <param name="mode">Mode to set</param>
        internal static void SetAfkTimer(bool mode)
        {
            if (Config.Osc.ShowAfkDuration && mode && _afkTimer == null)
            {
                Textbox.Notify(Config.Osc.AfkStartText, NotificationType.Afk);
                _afkStarted = DateTime.Now;
                _afkTimesChecked = 0;

                _afkTimer = new(Config.Osc.AfkDuration * 1000);
                _afkTimer.Elapsed += AfkTimerElapsed;
                _afkTimer.Start();

                Logger.Log("AFK Timer started");
                return;
            }
            else if (!mode && _afkTimer != null)
            {
                Textbox.Notify(Config.Osc.AfkEndText, NotificationType.Afk);
                _afkTimer.Stop();
                _afkTimer.Dispose();
                _afkTimer = null;
                Logger.Log("AFK Timer stopped");
            }
        }

        /// <summary>
        /// Event for the AFK timer elapsing
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private static void AfkTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (Config.Osc.AfkDoubleDuration > 0)
            {
                _afkTimesChecked++;

                var cycle = _afkTimesChecked / Config.Osc.AfkDoubleDuration;
                var modulo = Math.Pow(2, (int)Math.Log(cycle, 2));

                if (_afkTimesChecked % modulo != 0)
                    return;
            }

            Textbox.Notify($"{Config.Osc.AfkStatusText} {(e.SignalTime.AddMilliseconds(500) - _afkStarted).ToString(@"hh\:mm\:ss")}", NotificationType.Afk);
        }
        #endregion
    }
}
