using Hoscy.Services.OscControl;
using Hoscy.Services.Api;
using Hoscy.Ui.Pages;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using Hoscy.Services.Speech.Utilities;
using System.Text;

namespace Hoscy.Services.Speech
{
    internal readonly struct TextProcessor
    {
        static TextProcessor()
        {
            UpdateReplacementDataHandlers();
        }
        public TextProcessor() { }

        #region Replacements and Shortcuts
        private static IReadOnlyList<ReplacementHandler> _replacements = new List<ReplacementHandler>();
        private static IReadOnlyList<ShortcutHandler> _shortcuts = new List<ShortcutHandler>();

        internal static void UpdateReplacementDataHandlers()
        {
            var replacements = new List<ReplacementHandler>();
            foreach (var replacementData in Config.Speech.Replacements)
            {
                if (!replacementData.Enabled)
                    continue;

                try
                {
                    replacements.Add(new(replacementData));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Could not instantiate ReplacementHandler \"{replacementData.Text}\", RegEx might be broken");
                    replacementData.Enabled = false;
                }
            }

            var shortcuts = new List<ShortcutHandler>();
            foreach (var replacementData in Config.Speech.Shortcuts)
            {
                if (!replacementData.Enabled)
                    continue;

                try
                {
                    shortcuts.Add(new(replacementData));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Could not instantiate ShortcutHandler \"{replacementData.Text}\", RegEx might be broken");
                    replacementData.Enabled = false;
                }
            }

            _replacements = replacements;
            _shortcuts = shortcuts;
            Logger.PInfo($"Reloaded ReplacementDataHandlers ({_replacements.Count} Replacements, {_shortcuts.Count} Shortcuts)");
        }
        #endregion

        #region Processing
        internal bool UseTextbox { get; init; } = false;
        internal bool UseTts { get; init; } = false;
        internal bool TriggerCommands { get; init; } = false;
        internal bool TriggerReplace { get; init; } = false;
        internal bool AllowTranslate { get; init; } = false;

        /// <summary>
        /// Processes and sends strings with given options
        /// </summary>
        /// <param name="message">The message to process</param>
        internal void Process(string message)
            => ProcessInternal(message).RunWithoutAwait();

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

            if (TriggerCommands)
            {
                var result = ExecuteCommands(message.TrimEnd('.'));
                if (result != null)
                {
                    PageInfo.SetCommandMessage(message, result.Value);
                    return;
                }
            }

            //translation
            var translation = message;
            if (AllowTranslate && ((UseTextbox && Config.Api.TranslateTextbox) || (UseTts && Config.Api.TranslateTts)))
                translation = await Translator.Translate(message);

            if (UseTextbox)
            {
                if (Config.Api.TranslateTextbox)
                    Textbox.Say(translation + (Config.Api.AddOriginalAfterTranslate ? $" <> {message}" : string.Empty));
                else
                    Textbox.Say(message);
            }
            if (UseTts)
                Synthesizing.Say(Config.Api.TranslateTts ? translation : message);

            //Preprep for display in UI
            if (message != translation)
                message = $"{translation}\n\n{message}";
            if (message.Length > 512)
                message = message[..512];

            PageInfo.SetMessage(message, UseTextbox, UseTts);
        }

        /// <summary>
        /// Replaces message or parts of it
        /// </summary>
        private static string ReplaceMessage(string message)
        {
            //Splitting and checking for replacements
            foreach (var r in _replacements)
                message = r.Replace(message);

            var oldMessage = message;
            var shortcutTriggered = false;
            
            var shortcutSb = new StringBuilder();
            foreach (char c in message)
            {
                if (!Config.Speech.ShortcutIgnoredCharacters.Contains(c)) //todo: [TEST] Test if this works and is performant
                    shortcutSb.Append(c);
            }
            message = shortcutSb.ToString();
            
            //Checking for shortcuts
            foreach (var s in _shortcuts)
            {
                if (s.Compare(message))
                {
                    message = s.GetReplacement();
                    shortcutTriggered = true;
                    break;
                }
            }

            if (!shortcutTriggered)
                message = oldMessage;

            if (!message.StartsWith("[file]", StringComparison.OrdinalIgnoreCase))
                return message;

            return ExecuteFileCommand(message);
        }

        /// <summary>
        /// Executes "[file] ..." messages
        /// </summary>
        /// <param name="message">A message starting with "[file]"</param>
        private static string ExecuteFileCommand(string message)
        {
            var filePath = Regex.Replace(message, @"\[file\] *", string.Empty, RegexOptions.IgnoreCase);
            try
            {
                return string.Join(" ", File.ReadAllLines(filePath));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to read provided file, is the path correct and does HOSCY have access?");
                return string.Empty;
            }
        }

        /// <summary>
        /// Checking for message to be a command
        /// </summary>
        /// <returns>Success, null if no commands were ran</returns>
        private static bool? ExecuteCommands(string message)
        {
            message = message.Trim();
            var lowerMessage = message.ToLower();

            //Osc command handling
            if (lowerMessage.StartsWith("[osc]"))
                return Osc.ParseOscCommands(message);

            if (lowerMessage == "skip" || lowerMessage == "clear")
            {
                Logger.Info("Executing clear command");
                Textbox.Clear();
                Synthesizing.Skip();
                OscDataHandler.SetAfkTimer(false);
                return true;
            }

            if (lowerMessage.StartsWith("media "))
                return Media.HandleRawMediaCommand(lowerMessage);

            return null;
        }
        #endregion
    }
}
