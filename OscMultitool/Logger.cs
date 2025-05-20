using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Hoscy
{
    /// <summary>
    /// Class for logging of information in console
    /// </summary>
    internal static class Logger
    {
        private static readonly StreamWriter? _logWriter;
        private static readonly StreamWriter? _consoleWriter;

        //We use this to open a console window
        [DllImport("Kernel32")]
        private static extern void AllocConsole();

        #region Logger Start
        static Logger()
        {
            if (Config.Debug.OpenLogWindow)
                AllocConsole();

            (_consoleWriter, _logWriter) = StartLogger();
        }

        /// <summary>
        /// Starts the logging service
        /// </summary>
        /// <returns>Streamwriter for logging</returns>
        private static (StreamWriter?, StreamWriter?) StartLogger()
        {
            try
            {
                DeleteOldLogs();
                var logWriter = File.CreateText(Utils.PathLog);
                logWriter.AutoFlush = true;
                PInfo("Created logging file at " + Utils.PathLog);

                var consoleWriter = new StreamWriter(Console.OpenStandardOutput(), bufferSize: 16384)
                {
                    AutoFlush = true
                };

                return (consoleWriter, logWriter);
            }
            catch
            {
                return (null, null);
            }
        }

        private static void DeleteOldLogs()
        {
            var files = Directory.GetFiles(Utils.PathConfigFolder)
                    .Where(x => Path.GetFileName(x).StartsWith("log"))
                    .OrderByDescending(x => File.GetCreationTime(x)).ToArray();
            for (var i = 0; i < files.Length; i++)
                if (i > 1) File.Delete(files[i]);
        }
        #endregion

        #region Logging Function

        private static readonly object _lock = new();

        private static void Log(LogMessage message)
        {
            if (message.Severity >= LogSeverity.Error)
                App.OpenNotificationWindow(
                    "Error at: " + message.GetLocation(),
                    "An error has occured\nIf you are unsure why or how to handle it,\nplease open an issue on GitHub or Discord",
                    message.Message
                );

            if (message.Severity < Config.Debug.MinimumLogSeverity) return;

            lock (_lock) //Making sure log writing is not impacted by multithreading
            {
                if (!App.Running)
                    return;

                foreach (var filter in Config.Debug.LogFilters)
                    if (filter.Enabled && filter.Matches(message.Message))
                        return;

                Console.ForegroundColor = GetLogColor(message.Severity);
                var messageString = message.ToString().Replace("\n", " ").Replace("\r", "");
                _consoleWriter?.WriteLine(messageString);
                _logWriter?.WriteLine(messageString);
            }
        }

        internal static void Log(string message, LogSeverity severity = LogSeverity.Log, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(new(severity, file, member, line, message));
        internal static void Info(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.Info, file, member, line);
        internal static void PInfo(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.PrioInfo, file, member, line);
        internal static void Warning(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.Warning, file, member, line);
        internal static void Debug(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.Debug, file, member, line);

        // ERROR LOGGING
        internal static void Error(string message, bool notify = true, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0) //Error with basic message
        {
            var severity = notify ? LogSeverity.Error : LogSeverity.ErrSilent;
            Log(message, severity, file, member, line);
        }

        internal static void Error(Exception error, string descriptiveError = "", bool notify = true, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0) //Error using exception
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(descriptiveError))
                sb.AppendLine(descriptiveError + "\n\n--------------------------\nError details:\n");

            AppendExceptionToStringBuilder(error, sb);

            Error(sb.ToString(), notify, file, member, line);
        }

        private static void AppendExceptionToStringBuilder(Exception error, StringBuilder sb)
        {
            var errorType = error.GetType().Name;
            sb.AppendLine($"{errorType} at {error.Source}: {error.Message}");
            if (error.StackTrace is not null)
                sb.AppendLine($"\n--------------------------\nStack trace of {errorType}:\n\n" + error.StackTrace);
            if (error.InnerException is not null)
            {
                sb.AppendLine($"\n--------------------------\nInner exception of {errorType}:\n\n");
                AppendExceptionToStringBuilder(error.InnerException, sb);
            }
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
            LogSeverity.Warning => ConsoleColor.Yellow,
            LogSeverity.PrioInfo => ConsoleColor.Cyan,
            LogSeverity.Log => ConsoleColor.DarkGray,
            LogSeverity.Info => ConsoleColor.White,
            LogSeverity.Debug => ConsoleColor.DarkGray,
            _ => ConsoleColor.White
        };
        #endregion
    }

    /// <summary>
    /// Logging message class
    /// </summary>
    internal readonly struct LogMessage
    {
        private static readonly int _maxSeverityLength = 8;
        internal string SourceFile { get; private init; }
        internal string SourceMember { get; private init; }
        internal int SourceLine { get; private init; }
        internal string Message { get; private init; }
        internal LogSeverity Severity { get; private init; }
        private readonly string _sevString;
        internal DateTime Time { get; private init; }

        internal LogMessage(LogSeverity serverity, string sourceFile, string sourcMember, int sourceLine, string message)
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

        internal string GetLocation()
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
    internal class LoggerProxy<T> : ILogger<T>
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

            switch (logLevel)
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
    internal enum LogSeverity
    {
        Debug,
        Log,
        Info,
        PrioInfo,
        Warning,
        ErrSilent,
        Error
    }
}
