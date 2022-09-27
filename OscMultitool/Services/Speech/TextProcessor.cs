using Hoscy.OscControl;
using Hoscy.Services.Api;
using Hoscy.Ui.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hoscy.Services.Speech
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
            Logger.Debug("Processing message: " + message);

            if (TriggerReplace)
                message = ReplaceMessage(message);

            if (string.IsNullOrWhiteSpace(message))
                return;

            if (TriggerCommands && ExecuteCommands(message.TrimEnd('.')))
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
                if (Config.Textbox.TranslateTextbox)
                    Textbox.Say(translation + (Config.Textbox.AddOriginalAfterTranslate ? $" <> {message}" : string.Empty));
                else
                    Textbox.Say(message);
            }
            if (UseTts)
                Synthesizing.Say(Config.Speech.TranslateTts ? translation : message);

            if (message != translation)
                message = $"{translation}\n\n{message}";
            if (message.Length > 512)
                message = message[..512];

            PageInfo.SetMessage(message, UseTextbox, UseTts);
        }

        private static readonly List<Config.ReplacementModel> _shortcuts = Config.Speech.Shortcuts;
        private static readonly List<Config.ReplacementModel> _replacements = Config.Speech.Replacements;
        /// <summary>
        /// Replaces message or parts of it
        /// </summary>
        private string ReplaceMessage(string message)
        {
            //Splitting and checking for replacements
            foreach (var r in _replacements)
            {
                if(!r.Enabled) continue;

                RegexOptions opt = ReplaceCaseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None;
                message = Regex.Replace(message, $"\\b{r.Text}\\b", r.Replacement, opt);
            }

            var value = message;

            //Checking for shortcuts
            var checkMessage = ReplaceCaseInsensitive ? message.ToLower() : message;
            foreach (var s in _shortcuts)
            {
                if (s.Enabled && checkMessage == (ReplaceCaseInsensitive ? s.GetTextLc() : s.Text))
                {
                    value = s.Replacement;
                    break;
                }
            }

            if (File.Exists(value))
            {
                try
                {
                    return File.ReadAllText(value);
                } catch (Exception e)
                {
                    Logger.Error(e);
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
                Logger.Info("Executing clear command");
                Textbox.Clear();
                Synthesizing.Skip();
                return true;
            }

            if (lowerMessage.StartsWith("media "))
            {
                Media.HandleRawMediaCommand(lowerMessage.Replace("media ", ""));
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
        public static string? ExtractFromJson(string name, string json)
        {
            string regstring = name + @""" *: *""(?<value>([^""\\]|\\.)*)""";
            var regex = new Regex(regstring, RegexOptions.IgnoreCase);

            var result = regex.Match(json)?.Groups["value"].Value ?? null;
            return string.IsNullOrWhiteSpace(result) ? null : Regex.Unescape(result);
        }
        #endregion
    }
}
