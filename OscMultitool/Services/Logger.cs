using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace OscMultitool.Services
{
    /// <summary>
    /// Class for logging of information in console
    /// </summary>
    public static class Logger
    {
        private static readonly StreamWriter? _logWriter;

        //We use this to open a console window
        [DllImport("Kernel32")]
        private static extern void AllocConsole();

        #region Logger Start
        static Logger()
        {
            if (Config.Logging.OpenLogWindow)
                AllocConsole();

            _logWriter = StartLogger();
        }

        /// <summary>
        /// Starts the logging service
        /// </summary>
        /// <returns>Streamwriter for logging</returns>
        private static StreamWriter? StartLogger()
        {
            try
            {
                var writer = File.CreateText(Config.LogPath);
                writer.AutoFlush = true;
                PInfo("Created logging file at " + Config.LogPath, "Logging");
                return writer;
            }
            catch (Exception e)
            {
                Error(e, "Logger", false);
                return null;
            }
        }
        #endregion

        #region Logging Function

        private static readonly object _lock = new();
        public static void Log(LogMessage message)
        {
            if (!LogLevelAllowed(message.Severity)) return;

            lock (_lock) //Making sure log writing is not impacted by multithreading
            {
                if (!App.Running)
                    return;

                var lowerMessage = message.Message.ToLower();
                foreach (var filter in Config.Logging.LogFilter)
                    if (lowerMessage.Contains(filter))
                        return;

                var messageString = message.ToString().Replace('\n', ' ');
                Console.ForegroundColor = GetLogColor(message.Severity);
                Console.WriteLine(messageString);
                _logWriter?.WriteLine(messageString);
            }
        }
        public static void Log(string message, string source, LogSeverity severity = LogSeverity.Log)
            => Log(new(severity, source, message));

        // ERROR LOGGING
        public static void Error(string message, string source, bool window = true) //Error with basic message
        {
            Log(message.Replace("[s]", " "), source, LogSeverity.Error);
            if(window) //[s] token replaces with newlines or space
                MessageBox.Show($"{message.Replace("[s]", "\n")}\n\nIf you are unsure what to do with this, please open an issue on GitHub", "Error at " + source, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public static void Error(string type, string message, string source, string trace = "unspecified location", bool window = true) //Error using type
           => Error($"A {type} has occured:[s]{message}[s][s]{trace}", source, window);
        public static void Error(Exception error, string source, bool window = true) //Error using exception
            => Error(error.GetType().ToString(), error.Message, source, error.StackTrace ?? "unspecified location", window);

        // Other logging types
        public static void Info(string message, string source)
            => Log (message, source, LogSeverity.Info);
        public static void PInfo(string message, string source)
            => Log(message, source, LogSeverity.PrioInfo);
        public static void Warning(string message, string source)
            => Log(message, source, LogSeverity.Warning);
        public static void Debug(string message, string source)
            => Log(message, source, LogSeverity.Debug);
        #endregion

        #region Utils
        /// <summary>
        /// Get the color of the log message for the log window
        /// </summary>
        /// <param name="severity">Severity of log</param>
        /// <returns>Corresponding log color</returns>
        private static ConsoleColor GetLogColor(LogSeverity severity) => severity switch
        {
            LogSeverity.Error => ConsoleColor.Red,
            LogSeverity.Critical => ConsoleColor.DarkRed,
            LogSeverity.Warning => ConsoleColor.Yellow,
            LogSeverity.PrioInfo => ConsoleColor.Cyan,
            LogSeverity.Log => ConsoleColor.DarkGray,
            LogSeverity.Info => ConsoleColor.White,
            LogSeverity.Debug => ConsoleColor.DarkGray,
            _ => ConsoleColor.White
        };

        /// <summary>
        /// Check if logging is allowed for severity
        /// </summary>
        /// <param name="severity">Severity of log</param>
        /// <returns>Logging enabled?</returns>
        private static bool LogLevelAllowed(LogSeverity severity) => severity switch
        {
            LogSeverity.Error => Config.Logging.Error,
            LogSeverity.Warning => Config.Logging.Warning,
            LogSeverity.PrioInfo => Config.Logging.PrioInfo,
            LogSeverity.Info => Config.Logging.Info,
            LogSeverity.Log => Config.Logging.Log,
            LogSeverity.Debug => Config.Logging.Debug,
            _ => true
        };
        #endregion
    }

    /// <summary>
    /// Logging message class
    /// </summary>
    public struct LogMessage
    {
        public static int MaxSourceLength => 12;
        public static int MaxSeverityLength => 8;

        public string Source { get; private init; }
        public string Message { get; private init; }
        public LogSeverity Severity { get; private init; }
        private readonly string _sevString;
        public DateTime Time { get; private init; }

        public LogMessage(LogSeverity serverity, string source, string message)
        {
            Message = message;
            Source = Pad(source, MaxSourceLength);
            Severity = serverity;
            _sevString = Pad(serverity.ToString(), MaxSeverityLength);
            Time = DateTime.Now;
        }

        public override string ToString()
            => string.Join(' ', Time.ToString("HH:mm:ss.fff"), _sevString, Source, Message);

        /// <summary>
        /// Padding utility for log messages to have consistent width
        /// </summary>
        /// <param name="value">String to shorten</param>
        /// <param name="len">Length for padding</param>
        /// <returns></returns>
        private static string Pad(string value, int len)
        {
            if (value.Length > len)
                value = value[..len];
            value = value.PadRight(len, ' ');
            return value;
        }
    }

    public enum LogSeverity
    {
        Error,
        Warning,
        Info,
        Log,
        Debug,
        Critical,
        PrioInfo
    }
}
