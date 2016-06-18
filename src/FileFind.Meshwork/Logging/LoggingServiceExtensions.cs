using System;
using FileFind.Meshwork;
using FileFind.Meshwork.Logging;

namespace Meshwork.Logging
{
    public static class LoggingServiceExtensions
    {
        #region convenience methods (string message)

        public static void LogDebug(this ILoggingService service, string message)
        {
            service.Log(LogLevel.Debug, message);
        }

        public static void LogInfo(this ILoggingService service, string message)
        {
            service.Log(LogLevel.Info, message);
        }

        public static void LogWarning(this ILoggingService service, string message)
        {
            service.Log(LogLevel.Warn, message);
        }

        public static void LogError(this ILoggingService service, string message)
        {
            service.Log(LogLevel.Error, message);
        }

        public static void LogError(this ILoggingService service, Exception ex)
        {
            service.Log(LogLevel.Error, ex.ToString());
        }

        public static void LogFatalError(this ILoggingService service, string message)
        {
            service.Log(LogLevel.Fatal, message);
        }

        #endregion

        #region convenience methods (string messageFormat, params object[] args)

        public static void LogDebug(this ILoggingService service, string messageFormat, params object[] args)
        {
            service.Log(LogLevel.Debug, string.Format(messageFormat, args));
        }

        public static void LogInfo(this ILoggingService service, string messageFormat, params object[] args)
        {
            service.Log(LogLevel.Info, string.Format(messageFormat, args));
        }

        public static void LogWarning(this ILoggingService service, string messageFormat, params object[] args)
        {
            service.Log(LogLevel.Warn, string.Format(messageFormat, args));
        }

        public static void LogError(this ILoggingService service, string messageFormat, params object[] args)
        {
            service.Log(LogLevel.Error, string.Format(messageFormat, args));
        }

        public static void LogFatalError(this ILoggingService service, string messageFormat, params object[] args)
        {
            service.Log(LogLevel.Fatal, string.Format(messageFormat, args));
        }

        #endregion

        #region convenience methods (string message, Exception ex)

        public static void LogDebug(this ILoggingService service, string message, Exception ex)
        {
            service.Log(LogLevel.Debug, message + System.Environment.NewLine + (ex != null ? ex.ToString() : string.Empty));
        }

        public static void LogInfo(this ILoggingService service, string message, Exception ex)
        {
            service.Log(LogLevel.Info, message + System.Environment.NewLine + (ex != null ? ex.ToString() : string.Empty));
        }

        public static void LogWarning(this ILoggingService service, string message, Exception ex)
        {
            service.Log(LogLevel.Warn, message + System.Environment.NewLine + (ex != null ? ex.ToString() : string.Empty));
        }

        public static void LogError(this ILoggingService service, string message, Exception ex)
        {
            service.Log(LogLevel.Error, message + System.Environment.NewLine + (ex != null ? ex.ToString() : string.Empty));
        }

        public static void LogFatalError(this ILoggingService service, string message, Exception ex)
        {
            service.Log(LogLevel.Fatal, message + System.Environment.NewLine + (ex != null ? ex.ToString() : string.Empty));
        }

        #endregion

    }
}

