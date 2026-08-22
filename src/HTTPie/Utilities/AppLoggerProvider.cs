// Copyright (c) Weihan Li. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using WeihanLi.Common.Services;

namespace HTTPie.Utilities;

internal sealed class AppLoggerProvider(Action<string, LogLevel, Exception?, string> logAction) : ILoggerProvider
{
    internal static ILoggerProvider Default { get; } = new AppLoggerProvider((category, level, exception, msg) =>
    {
        var (foregroundColor, backgroundColor) = GetConsoleColorForLogLevel(level);
        var levelText = GetLogLevelText(level);
        var dateTime = DateTimeOffset.Now;
        var message = @$"[{levelText}][{category}] {dateTime} {msg}";
        if (exception is not null)
        {
            message = $"{message}{Environment.NewLine}{exception}";
        }

        ConsoleHelper.WriteLineWithColor(message, foregroundColor, backgroundColor);
        if (level is LogLevel.Trace)
        {
            Trace.WriteLine(message);
        }

        return;

        static (ConsoleColor? ForegroundColor, ConsoleColor? BackgroundColor) GetConsoleColorForLogLevel(LogLevel logLevel)
            => logLevel switch
            {
                LogLevel.Trace or LogLevel.Debug => (ConsoleColor.DarkGray, ConsoleColor.Black),
                LogLevel.Information => (ConsoleColor.DarkGreen, ConsoleColor.Black),
                LogLevel.Warning => (ConsoleColor.Yellow, ConsoleColor.Black),
                LogLevel.Error => (ConsoleColor.Black, ConsoleColor.DarkRed),
                LogLevel.Critical => (ConsoleColor.White, ConsoleColor.DarkRed),
                _ => (null, null)
            };

        static string GetLogLevelText(LogLevel logLevel)
            => logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => logLevel.ToString().ToLowerInvariant()
            };
    });

    private readonly ConcurrentDictionary<string, AppLogger> _loggers = new();

    public void Dispose()
    {
        _loggers.Clear();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, category => new AppLogger(category, logAction));
    }

    private sealed class AppLogger(string categoryName, Action<string, LogLevel, Exception?, string> logAction) : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var msg = formatter(state, exception);
            logAction.Invoke(categoryName, logLevel, exception, msg);
        }

        public bool IsEnabled(LogLevel logLevel) => true;


        IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;
    }
}
