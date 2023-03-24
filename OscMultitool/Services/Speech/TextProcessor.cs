using Hoscy.Services.OscControl;
using Hoscy.Services.Api;
using Hoscy.Ui.Pages;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hoscy.Services.Speech
{
    internal class TextProcessor
    {
        internal bool UseTextbox { get; init; } = false;
        internal bool UseTts { get; init; } = false;
        internal bool TriggerCommands { get; init; } = false;
        internal bool TriggerReplace { get; init; } = false;
        internal bool AllowTranslate { get; init; } = false;

        #region Processing
        /// <summary>
        /// Processes and sends strings with given options
        /// </summary>
        /// <param name="message">The message to process</param>
        internal void Process(string message)
            => Utils.RunWithoutAwait(ProcessInternal(message));

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
                var resultMessage = ExecuteCommands(message.TrimEnd('.'));
                if (resultMessage != null)
                {
                    PageInfo.SetCommandMessage(resultMessage);
                    return;
                }
            }

            //translation
            var translation = message;
            if (AllowTranslate && ((UseTextbox && Config.Api.TranslateTextbox) || (UseTts && Config.Api.TranslateTts)))
                translation = await Translation.Translate(message);

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
        private string ReplaceMessage(string message)
        {
            //Splitting and checking for replacements
            foreach (var r in Config.Speech.Replacements)
            {
                if (r.Enabled)
                    message = r.Replace(message);
            }

            //Checking for shortcuts
            foreach (var s in Config.Speech.Shortcuts)
            {
                if (s.Enabled && s.Compare(message))
                {
                    message = s.Replacement;
                    break;
                }
            }

            if (!message.StartsWith("[file]", StringComparison.OrdinalIgnoreCase))
                return message;

            var filePath = Regex.Replace(message, @"\[file\] *", "", RegexOptions.IgnoreCase);
            if (File.Exists(filePath))
            {
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
            else
                return filePath;
        }

        /// <summary>
        /// Checking for message to be a command
        /// </summary>
        /// <returns>Was a command executed?</returns>
        private static string? ExecuteCommands(string message)
        {
            message = message.Trim();
            var lowerMessage = message.ToLower();

            //Osc command handling
            if (lowerMessage.StartsWith("[osc]"))
                return Osc.ParseOscCommands(message) ? message : "Failed to execute OSC:\n\n" + message;

            if (lowerMessage == "skip" || lowerMessage == "clear")
            {
                Logger.Info("Executing clear command");
                Textbox.Clear();
                Synthesizing.Skip();
                OscDataHandler.SetAfkTimer(false);
                return message;
            }

            if (lowerMessage.StartsWith("media "))
            {
                Media.HandleRawMediaCommand(lowerMessage.Replace("media ", ""));
                return message;
            }

            return null;
        }
        #endregion
    }
}
