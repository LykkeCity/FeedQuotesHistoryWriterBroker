using System;

namespace Lykke.Service.QuotesHistory.Settings
{
    public class StorageRetrySettings
    {
        public int OnModificationsRetryCount { get; set; }
        public int OnGettingRetryCount { get; set; }
        public TimeSpan RetryDelay { get; set; }
    }
}