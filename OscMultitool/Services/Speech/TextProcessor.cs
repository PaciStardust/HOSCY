using OscMultitool.OscControl;
using OscMultitool.Services.Api;
using OscMultitool.Ui.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OscMultitool.Services.Speech
{
    public class TextProcessor
    {
        public bool UseTextbox { get; init; } = false;
        public bool UseTts { get; init; } = false;
        public bool TriggerCommands { get; init; } = false;
        public bool TriggerReplace { get; init; } = false;
        public bool ReplaceCaseInsensitive { get; init; } = false;
        public bool AllowTranslate { get; init; } = false;

        #region Processing
        /// <summary>
        /// Processes and sends strings with given options
        /// </summary>
        /// <param name="message">The message to process</param>
        public void Process(string message)
            => Task.Run(async () => await ProcessInternal(message)).ConfigureAwait(false);

        /// <summary>
        /// Processes and sends strings with given options
        /// </summary>
        /// <param name="message">The message to process</param>
        private async Task ProcessInternal(string message)
        {
            Logger.Debug("Processing message: " + message, "TxtProcessor");

            if (TriggerReplace)
                message = ReplaceMessage(message);

            if (string.IsNullOrWhiteSpace(message))
                return;

            if (TriggerCommands && ExecuteCommands(message))
            {
                PageInfo.SetCommandMessage(message);
                return;
            }

            //translation
            var translation = message;
            if (AllowTranslate && ((UseTextbox && Config.Textbox.TranslateTextbox) || (UseTts && Config.Speech.TranslateTts)))
                translation = await Translation.Translate(message);

            if (UseTextbox)
            {
                if (Config.Textbox.AddOriginalAfterTranslate)
                    Textbox.Say($"{translation} <> {message}");
                else
                    Textbox.Say(translation);
            }
            if (UseTts)
                Synthesizing.Say(translation);

            if (message != translation)
                message = $"{translation}\n\n{message}";
            if (message.Length > 512)
                message = message[..512];

            PageInfo.SetMessage(message, UseTextbox, UseTts);
        }

        private static readonly Dictionary<string, string> _shortcuts = Config.Speech.Shortcuts;
        private static readonly Dictionary<string, string> _replacements = Config.Speech.Replacements;
        /// <summary>
        /// Replaces message or parts of it
        /// </summary>
        private string ReplaceMessage(string message)
        {
            //Splitting and checking for replacements
            foreach (var key in _replacements.Keys)
            {
                RegexOptions opt = ReplaceCaseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None;
                message = Regex.Replace(message, key, _replacements[key], opt);
            }

            var value = message;

            //Checking for shortcuts
            var checkMessage = ReplaceCaseInsensitive ? message.ToLower() : message;
            if (_shortcuts.ContainsKey(checkMessage))
                value = _shortcuts[checkMessage];

            if (File.Exists(value))
            {
                try
                {
                    return File.ReadAllText(value);
                } catch (Exception e)
                {
                    Logger.Error(e, "TextProcess");
                    return string.Empty;
                }
            }
            else
                return value;
        }

        /// <summary>
        /// Checking for message to be a command
        /// </summary>
        /// <returns>Was a command executed?</returns>
        private static bool ExecuteCommands(string message)
        {
            message = message.Trim();
            var lowerMessage = message.ToLower();

            //Osc command handling
            if (lowerMessage.StartsWith("[osc]"))
            {
                Osc.ParseOscCommands(message);
                return true;
            }

            if (lowerMessage == "skip" || lowerMessage == "clear")
            {
                Logger.Info("Executing clear command", "TxtProcessor");
                Textbox.Clear();
                Synthesizing.Skip();
                return true;
            }

            var keyCheck = (Config.Speech.MediaControlKeyword + " ").ToLower();
            if (!string.IsNullOrWhiteSpace(keyCheck) && lowerMessage.StartsWith(keyCheck))
            {
                Media.HandleRawMediaCommand(lowerMessage.Replace(keyCheck, ""));
                return true;
            }

            return false;
        }
        #endregion

        #region Static Utility
        /// <summary>
        /// Extracts a json field from a string
        /// </summary>
        /// <param name="name">Name of the field to search</param>
        /// <param name="json">The text inside the field or string.Empty if unavailable</param>
        /// <returns></returns>
        public static string ExtractFromJson(string name, string json)
        {
            string regstring = name + @""" *: *""(?<value>[^""]*)""";
            var regex = new Regex(regstring, RegexOptions.IgnoreCase);

            return regex.Match(json)?.Groups["value"].Value ?? string.Empty;
        }
        #endregion
    }
}
