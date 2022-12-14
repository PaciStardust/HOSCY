using Hoscy.Services.OscControl;
using Hoscy.Ui.Pages;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Hoscy.Services.Speech
{
    public static class Textbox
    {
        private static string _notification = string.Empty;
        private static NotificationType _notificationType = NotificationType.None;
        private static NotificationType _notificationTypeLast = NotificationType.None;

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
        private static readonly int _minimumTimeoutMs = 1250;
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

                //If the intended timeout has passed, messages and autoclear can happen
                //this will only fail because of notification overwriting
                if (DateTime.Now >= _intendedTimeout)
                {
                    //Set from message
                    if (MessageQueue.Count > 0)
                    {
                        message = MessageQueue.Dequeue();
                        notify = Config.Textbox.SoundOnMessage;
                        _autocleared = !Config.Textbox.AutomaticClearMessage;
                    }
                    //Allow notifcaton
                    else if (_notificationType != NotificationType.None)
                    {
                        sendNotification = true;
                    }
                    //Autoclear
                    else if (!_autocleared)
                    {
                        //Early timeout
                        SendMessage(string.Empty, false);
                        _autocleared = true;
                        Thread.Sleep(_minimumTimeoutMs);
                        continue;
                    }
                }
                //Notification override is triggered
                else if (lastSentNotif && _notificationType != NotificationType.None && _notificationType == _notificationTypeLast) 
                {
                    sendNotification = true;
                    Logger.Debug("Notification timeout was shortened due to equal type");
                }

                //Notification is set, moved to not have duplicates
                if (sendNotification)
                {
                    message = _notification;
                    ClearNotification();
                    notify = Config.Textbox.SoundOnNotification;
                    _autocleared = !Config.Textbox.AutomaticClearNotification;
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
                        Logger.Info($"Sent message with timeout {threadSleep}: {message}");
                }

                Thread.Sleep(threadSleep);
            }
        }

        private static bool SendMessage(string message, bool notify)
        {
            var packet = new OscPacket("/chatbox/input", message, true, notify);
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

            input = input.Length > Config.Textbox.MaxLength - 2
                ? input[..(Config.Textbox.MaxLength-5)] + "..."
                : input;

            input = $"〈{input}〉";

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
        /// Clears the queue and box
        /// </summary>
        public static void Clear()
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
        Counter,
        Afk,
        External
    }
}
