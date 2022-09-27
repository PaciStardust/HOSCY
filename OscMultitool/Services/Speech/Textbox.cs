using Hoscy.OscControl;
using Hoscy.Ui.Pages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Hoscy.Services.Speech
{
    public static class Textbox
    {
        private static string _notification = string.Empty;
        private static NotificationType _notificationType = NotificationType.None;

        private static readonly Queue<string> MessageQueue = new();

        /// <summary>
        /// Initializing the Message loop on first call of class
        /// </summary>
        static Textbox()
        {
            Thread loopThread = new(new ThreadStart(MessageQueueLoop))
            {
                Name = "Textbox",
                IsBackground = true
            };
            loopThread.Start();

            Logger.PInfo("Started textbox thread");
        }

        #region Message Handling
        private static bool _autocleared = true;
        /// <summary>
        /// Loop for sending messages
        /// </summary>
        private static void MessageQueueLoop()
        {
            while (App.Running)
            {
                //Wait if Q empty
                var message = string.Empty;
                var timeout = 10;

                if (MessageQueue.Count > 0)
                {
                    message = MessageQueue.Dequeue();
                    timeout = GetMessageTimeout(message);
                    _autocleared = !Config.Textbox.AutomaticClearMessage;
                }
                else if (_notificationType != NotificationType.None)
                {
                    message = _notification;
                    ClearNotification();
                    timeout = GetMessageTimeout(message);
                    _autocleared = !Config.Textbox.AutomaticClearNotification;
                }
                else if (!_autocleared)
                {
                    SendMessage(string.Empty);
                    timeout = 1000;
                    _autocleared = true;
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    if (!SendMessage(message))
                    {
                        _autocleared = true;
                        continue;
                    }

                    Logger.Info($"Sent message with timeout {timeout}: {message}");
                }

                Thread.Sleep(timeout);
            }
        }

        private static bool SendMessage(string message)
        {
            var packet = new OscPacket("/chatbox/input", ReplaceSpecialCharacters(message), true);
            if (!packet.IsValid)
            {
                Logger.Warning("Unable to send message to chatbox, packet is invalid");
                return false;
            }
            Osc.Send(packet);
            return true;
        }
        #endregion

        #region Sentence Processing
        /// <summary>
        /// Handle splitting and adding to queue
        /// </summary>
        public static void Say(string input)
        {
            //Checks for disabled textbox, empty input or command
            if (string.IsNullOrWhiteSpace(input))
                return;

            foreach (var message in SplitMessage(input))
            {
                MessageQueue.Enqueue(message);
                Logger.Log($"Added to MessageQueue (Q:{MessageQueue.Count},L:{message.Length}): {message}");
            }
        }

        /// <summary>
        /// Sets the notification
        /// </summary>
        /// <param name="input">Message</param>
        /// <param name="type">Type of Notification</param>
        public static void Notify(string input, NotificationType type)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                if (_notificationType == type)
                    ClearNotification();
                return;
            }

            input = input.Length > Config.Textbox.MaxLength ? input[..130] : input;

            _notificationType = type;
            _notification = input;
            PageInfo.SetNotification(input, type);
            Logger.Log("Setting notification to: " + input);
        }

        /// <summary>
        /// Splitting message
        /// </summary>
        /// <returns>Split message</returns>
        private static List<string> SplitMessage(string message)
        {
            var maxLen = Config.Textbox.MaxLength;

            var words = message.Split(' ');
            var messages = new List<string>();
            var currentMessage = string.Empty;
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                word = word.Length > maxLen ? word[..maxLen] : word;

                var newMessage = string.Join(" ", currentMessage, word).Trim();

                //Message short enough
                if (newMessage.Length <= maxLen)
                {
                    currentMessage = newMessage;
                    continue;
                }

                messages.Add(currentMessage + " ...");
                currentMessage = "... " + word;
            }

            if (!string.IsNullOrWhiteSpace(currentMessage))
                messages.Add(currentMessage);

            return messages;
        }

        /// <summary>
        /// Replacing special language characters for textbox
        /// </summary>
        private static string ReplaceSpecialCharacters(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);

            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Clears the queue and box
        /// </summary>
        public static void Clear()
        {
            MessageQueue.Clear();
            ClearNotification();
            _autocleared = false;
            Logger.Info("Clearing message queue");
        }

        /// <summary>
        /// Clears the notification
        /// </summary>
        private static void ClearNotification()
        {
            _notification = string.Empty;
            _notificationType = NotificationType.None;
        }
        #endregion

        #region Utility
        private static DateTime _lastEnabled = DateTime.MinValue;
        private static bool _lastSet = false;

        /// <summary>
        /// Enables typing indicator for textbox
        /// Note: This only stays on for 5 seconds ingame
        /// </summary>
        /// <param name="mode"></param>
        public static void EnableTyping(bool mode)
        {
            if (!mode && !_lastSet)
                return;

            if (mode)
            {
                if ((!Config.Speech.UseTextbox && !Config.Textbox.UseIndicatorWithoutBox) || _lastEnabled + new TimeSpan(0,0,4) > DateTime.Now)
                    return;

                _lastEnabled = DateTime.Now;
            }

            _lastSet = mode;

            var packet = new OscPacket("/chatbox/typing", mode ? 1 : 0);
            if (!packet.IsValid)
            {
                Logger.Warning("Unable to set chatbox typing status, package is invalid");
                return;
            }
            Osc.Send(packet);
        }

        /// <summary>
        /// Calculates the dynamic timeout or returns the default timeout
        /// </summary>
        /// <param name="message">Message to calculate timeout for</param>
        /// <returns>Milliseconds of timeout</returns>
        private static int GetMessageTimeout(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return 1000; //to avoid hitting ratelimit

            if (!Config.Textbox.DynamicTimeout)
                return Config.Textbox.DefaultTimeout;

            var timeout = (int)(Math.Ceiling(message.Length / 20f) * Config.Textbox.TimeoutMultiplier);
            return Math.Max(timeout, Config.Textbox.MinimumTimeout);
        }
        #endregion
    }

    public enum NotificationType
    {
        None,
        Media,
        External
    }
}
