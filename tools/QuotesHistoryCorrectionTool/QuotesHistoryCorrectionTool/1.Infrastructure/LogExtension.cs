using System;
using System.Threading.Tasks;
using Common.Log;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

// ReSharper disable once CheckNamespace
namespace QuotesHistoryCorrectionTool.Infrastructure
{
    public static class LogExtension
    {
        private static TimeSpan _infoTimeout = TimeSpan.FromSeconds(0);
        private static DateTime _lastInsertLoggingTime = DateTime.MinValue;
        private static DateTime _lastDeleteLoggingTime = DateTime.MinValue;

        public static void SetInfoMessagesTimeout(this ILog log, TimeSpan timeout)
        {
            _infoTimeout = timeout;
        }

        public static async Task WriteInfoAsync(this ILog log, string process, string context, string message, LoggingOperation operation, bool useThrottling)
        {
            if (useThrottling && DateTime.UtcNow - _lastInsertLoggingTime < _infoTimeout && operation == LoggingOperation.Insert)
                return;
            if (useThrottling && DateTime.UtcNow - _lastDeleteLoggingTime < _infoTimeout && operation == LoggingOperation.Delete)
                return;
            
            await log.WriteInfoAsync(process, context, message, DateTime.UtcNow);

            switch (operation)
            {
                case LoggingOperation.Insert:
                    _lastInsertLoggingTime = DateTime.UtcNow;
                    break;

                case LoggingOperation.Delete:
                    _lastDeleteLoggingTime = DateTime.UtcNow;
                    break;

                default:
                    throw new ArgumentException($"Unrecognized operation for logging: {operation.ToString()}");
            }
        }

        public enum LoggingOperation
        {
            Insert,
            Delete
        }
    }
}
