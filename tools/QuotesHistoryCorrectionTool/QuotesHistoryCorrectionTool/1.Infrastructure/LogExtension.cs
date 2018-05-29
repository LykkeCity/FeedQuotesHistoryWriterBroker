using System;
using System.Threading.Tasks;
using Common.Log;

// ReSharper disable once CheckNamespace
namespace QuotesHistoryCorrectionTool.Infrastructure
{
    public static class LogExtension
    {
        private static TimeSpan _infoTimeout = TimeSpan.FromSeconds(0);
        private static DateTime _lastLoggingTime = DateTime.MinValue;

        public static void SetInfoMessagesTimeout(this ILog log, TimeSpan timeout)
        {
            _infoTimeout = timeout;
        }

        public static async Task WriteInfoAsync(this ILog log, string process, string context, string message, bool useThrottling)
        {
            if (useThrottling && DateTime.UtcNow - _lastLoggingTime < _infoTimeout)
                return;

            await log.WriteInfoAsync(process, context, message, DateTime.UtcNow);

            _lastLoggingTime = DateTime.UtcNow;
        }
    }
}
