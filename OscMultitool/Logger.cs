using Hoscy.Ui;
using Hoscy.Ui.Windows;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace Hoscy
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
            if (Config.Debug.OpenLogWindow)
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
                PInfo("Created logging file at " + Config.LogPath);
                return writer;
            }
            catch (Exception e)
            {
                Error(e, notify:false);
                return null;
            }
        }
        #endregion

        #region Logging Function

        private static readonly object _lock = new();

        private static void Log(LogMessage message)
        {
            if (message.Severity == LogSeverity.Error)
                OpenNotificationWindow(
                    "Error at: " + message.GetLocation(),
                    "An error has occured\nIf you are unsure why or how to handle it,\nplease open an issue on GitHub or Discord",
                    message.Message
                );


            if (!LogLevelAllowed(message.Severity)) return;

            lock (_lock) //Making sure log writing is not impacted by multithreading
            {
                if (!App.Running)
                    return;

                var lowerMessage = message.Message.ToLower();
                foreach (var filter in Config.Debug.LogFilter)
                    if (lowerMessage.Contains(filter))
                        return;

                var messageString = message.ToString().Replace("\n", " ").Replace("\r", "");
                Console.ForegroundColor = GetLogColor(message.Severity);
                Console.WriteLine(messageString);
                _logWriter?.WriteLine(messageString);
            }
        }
        public static void Log(string message, LogSeverity severity = LogSeverity.Log, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(new(severity, file, member, line, message));

        public static void Info(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.Info, file, member, line);
        public static void PInfo(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.PrioInfo, file, member, line);
        public static void Warning(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.Warning, file, member, line);
        public static void Debug(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.Debug, file, member, line);

        // ERROR LOGGING
        public static void Error(string message, bool notify = true, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0) //Error with basic message
        {
            var severity = notify ? LogSeverity.Error : LogSeverity.ErrSilent;
            Log(message, severity, file, member, line);
        }
        
        public static void Error(Exception error, string descriptiveError = "", bool notify = true, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0) //Error using exception
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(descriptiveError))
                sb.AppendLine(descriptiveError + "\n\n--------------------------\nError details:\n");

            sb.AppendLine(error.Message);

            if (error.InnerException != null)
                sb.AppendLine($"\n(Inner {error.InnerException.GetType()}: {error.Message}{(error.Source == null ? "" : $" at {error.Source}")})");

            if (error.StackTrace != null)
                sb.AppendLine("\n--------------------------\nStack trace:\n\n" + error.StackTrace);

            Error(sb.ToString(), notify, file, member, line);
        }
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
            LogSeverity.ErrSilent => ConsoleColor.Red,
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
            LogSeverity.Error => Config.Debug.Error,
            LogSeverity.ErrSilent => Config.Debug.Error,
            LogSeverity.Warning => Config.Debug.Warning,
            LogSeverity.PrioInfo => Config.Debug.PrioInfo,
            LogSeverity.Info => Config.Debug.Info,
            LogSeverity.Log => Config.Debug.Log,
            LogSeverity.Debug => Config.Debug.Debug,
            _ => true
        };

        /// <summary>
        /// Creates a notification window
        /// </summary>
        /// <param name="title">Title of window</param>
        /// <param name="subtitle">Text above notification</param>
        /// <param name="notification">Contents of notification box</param>
        public static void OpenNotificationWindow(string title, string subtitle, string notification, bool locking = false)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new NotificationWindow(title, subtitle, notification);
                window.SetDarkMode(true);
                if (locking)
                    window.ShowDialog();
                else
                    window.Show();
            });
        }
        #endregion
    }

    /// <summary>
    /// Logging message class
    /// </summary>
    public readonly struct LogMessage
    {
        private static readonly int _maxSeverityLength = 8;
        public string SourceFile { get; private init; }
        public string SourceMember { get; private init; }
        public int SourceLine { get; private init; }
        public string Message { get; private init; }
        public LogSeverity Severity { get; private init; }
        private readonly string _sevString;
        public DateTime Time { get; private init; }

        public LogMessage(LogSeverity serverity, string sourceFile, string sourcMember, int sourceLine, string message)
        {
            Message = message;
            SourceFile = Path.GetFileName(sourceFile);
            SourceMember = sourcMember;
            SourceLine = sourceLine;
            Severity = serverity;
            _sevString = Pad(serverity.ToString(), _maxSeverityLength);
            Time = DateTime.Now;
        }

        public override string ToString()
            => $"{Time:HH:mm:ss.fff} {_sevString} [{GetLocation()}] {Message}";

        public string GetLocation()
            => $"{SourceFile}::{SourceMember}:{SourceLine}";

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

    /// <summary>
    /// The only reason this exists is so I can log OSCQuery
    /// </summary>
    public class LoggerProxy<T> : ILogger<T>
    {
#nullable disable
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
#nullable enable

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string? message = state?.ToString();
            if (string.IsNullOrWhiteSpace(message))
                return;

            switch(logLevel)
            {
                case LogLevel.Information:
                    Logger.Info(message);
                    return;
                case LogLevel.Warning:
                    Logger.Warning(message);
                    return;
                case LogLevel.Error:
                    Logger.Error(message);
                    return;
                case LogLevel.Critical:
                    Logger.Error(message);
                    return;

                default:
                    Logger.Debug(message);
                    return;
            }
        }
    }
    public enum LogSeverity
    {
        Error,
        ErrSilent,
        Warning,
        Info,
        Log,
        Debug,
        Critical,
        PrioInfo
    }
}
