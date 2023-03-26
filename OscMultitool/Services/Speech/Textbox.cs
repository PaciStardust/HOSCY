using Hoscy.Services.OscControl;
using Hoscy.Ui.Pages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Hoscy.Services.Speech
{
    internal static class Textbox
    {
        private static string _notification = string.Empty;
        private static NotificationType _notificationType = NotificationType.None;
        private static NotificationType _notificationTypeLast = NotificationType.None;

        private static readonly Queue<string> MessageQueue = new();

        private static readonly int _minimumTimeoutMs = 1250;

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
        private static DateTime _intendedTimeout = DateTime.MinValue; //Used to ensure notif override
        /// <summary>
        /// Loop for sending messages
        /// </summary>
        private static void MessageQueueLoop()
        {
            //Flag for checking if override behaviour is allowed
            var lastSentNotif = false;

            while (App.Running)
            {
                var message = string.Empty;
                var notify = false;
                var threadSleep = 10;
                var sendNotification = false;

                //Messages are available, also stops notifications from loading unless there is none
                //Only sends once timeout has passed OR the last sent was a notification and skip is enabled
                if (MessageQueue.Count > 0)
                {
                    var timeoutBypass = lastSentNotif && Config.Textbox.UseNotificationSkip;
                    if (DateTime.Now >= _intendedTimeout || timeoutBypass)
                    {
                        if (timeoutBypass)
                            Logger.Debug("Notification timeout was shortened due to incoming message");

                        message = MessageQueue.Dequeue();
                        notify = Config.Textbox.SoundOnMessage;
                        _autocleared = !Config.Textbox.AutomaticClearMessage;
                    }
                }
                //Notification is available
                //Only sends once timeout has passed OR the last sent was a notification and of the same type
                else if (_notificationType != NotificationType.None)
                {
                    var timeoutBypass = lastSentNotif && _notificationType == _notificationTypeLast;
                    if (DateTime.Now >= _intendedTimeout || timeoutBypass)
                    {
                        if (timeoutBypass)
                            Logger.Debug("Notification timeout was shortened due to equal type");

                        message = _notification;
                        ClearNotification();
                        notify = Config.Textbox.SoundOnNotification;
                        _autocleared = !Config.Textbox.AutomaticClearNotification;
                        sendNotification = true;
                    }
                }
                //Automatically clears if needed
                //Only sends once timeout has passed
                else if (!_autocleared && DateTime.Now >= _intendedTimeout)
                {
                    //Early timeout
                    SendMessage(string.Empty, false);
                    _autocleared = true;
                    Thread.Sleep(_minimumTimeoutMs);
                    continue;
                }

                //Actual sending
                if (!string.IsNullOrWhiteSpace(message))
                {
                    //If failed, still set autoclear
                    if (!SendMessage(message, notify))
                    {
                        _autocleared = true;
                        continue;
                    }

                    var msgTimeout = GetMessageTimeout(message);
                    _intendedTimeout = DateTime.Now.AddMilliseconds(msgTimeout);
                    threadSleep = _minimumTimeoutMs;
                    lastSentNotif = sendNotification;

                    if (sendNotification)
                        Logger.Info($"Sent notification with timeout {threadSleep}-{msgTimeout}: {message}");
                    else
                        Logger.Info($"Sent message with timeout {msgTimeout}: {message}");
                }

                Thread.Sleep(threadSleep);
            }
        }

        private static bool SendMessage(string message, bool notify)
        {
            if (message.Length > 140)
                message = message[..140];

            var packet = new OscPacket(Config.Osc.AddressGameTextbox, message, true, notify);
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
        internal static void Say(string input)
        {
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
        internal static void Notify(string input, NotificationType type)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                if (_notificationType == type)
                    ClearNotification();
                return;
            }

            if (Config.Textbox.UseNotificationPriority && type < _notificationType)
            {
                Logger.Debug("Did not override notification, priority too low");
                return;
            }

            var indLen = Config.Textbox.NotificationIndicatorLength();
            input = input.Length > Config.Textbox.MaxLength - indLen
                ? input[..(Config.Textbox.MaxLength-indLen-3)] + "..."
                : input;

            input = $"{Config.Textbox.NotificationIndicatorLeft}{input}{Config.Textbox.NotificationIndicatorRight}";

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
            var currentMessage = new StringBuilder();

            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (word.Length > maxLen)
                    word = word[..maxLen];

                if (word.Length + currentMessage.Length <= maxLen)
                {
                    if (currentMessage.Length != 0)
                        currentMessage.Append(' ');
                    currentMessage.Append(word);
                    continue;
                }

                currentMessage.Append(" ...");
                messages.Add(currentMessage.ToString());
                currentMessage.Clear().Append($"... {word}");
            }

            var messageStringLast = currentMessage.ToString();
            if (!string.IsNullOrWhiteSpace(messageStringLast))
                messages.Add(messageStringLast);

            return messages;
        }

        /// <summary>
        /// Clears the queue and box
        /// </summary>
        internal static void Clear()
        {
            MessageQueue.Clear();
            ClearNotification();
            _autocleared = false;
            _intendedTimeout = DateTime.MinValue;
            Logger.Info("Clearing message queue");
        }

        /// <summary>
        /// Clears the notification
        /// </summary>
        private static void ClearNotification()
        {
            _notification = string.Empty;
            _notificationTypeLast = _notificationType;
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
        internal static void EnableTyping(bool mode)
        {
            if (!mode && !_lastSet)
                return;

            if (mode)
            {
                if ((!Config.Speech.UseTextbox && !Config.Textbox.UseIndicatorWithoutBox) || _lastEnabled.AddSeconds(4) > DateTime.Now)
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
                return _minimumTimeoutMs; //to avoid hitting ratelimit

            if (!Config.Textbox.DynamicTimeout)
                return Config.Textbox.DefaultTimeout;

            var timeout = (int)(Math.Ceiling(message.Length / 20f) * Config.Textbox.TimeoutMultiplier);
            return Math.Max(timeout, Config.Textbox.MinimumTimeout);
        }
        #endregion
    }

    internal enum NotificationType
    {
        None,
        Counter,
        Media,
        Afk,
        External
    }
}
