using System;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;

namespace Lykke.Service.QuotesHistory.Core.Settings
{
    public class AppSettings
    {
        public SlackNotificationsSettings SlackNotifications { get; set; } = new SlackNotificationsSettings();
        public QuotesHistorySettings QuotesHistoryService { get; set; } = new QuotesHistorySettings();
        public AssetsSettings Assets { get; set; }

        public class QuotesHistorySettings
        {
            public RabbitMqSettings RabbitMq { get; set; } = new RabbitMqSettings();
            public ConnectionStringsSettings ConnectionStrings { get; set; } = new ConnectionStringsSettings();
            public StorageRetrySettings StorageRetrySettings { get; set; } = new StorageRetrySettings();
   
        }

        public class RabbitMqSettings
        {
            public string ConnectionString { get; set; }
        }

        public class ConnectionStringsSettings
        {
            public string HistoryConnectionString { get; set; }
            public string LogsConnectionString { get; set; }
        }

        public class SlackNotificationsSettings
        {
            public AzureQueueSettings AzureQueue { get; set; } = new AzureQueueSettings();
        }

        public class AzureQueueSettings
        {
            public string ConnectionString { get; set; }

            public string QueueName { get; set; }
        }

        public class AssetsSettings
        {
            public string ServiceUrl { get; set; }
            public TimeSpan CacheExpirationPeriod { get; set; }
        }

        public class StorageRetrySettings
        {
            public int OnModificationsRetryCount { get; set; }
            public int OnGettingRetryCount { get; set; }
            public TimeSpan RetryDelay { get; set; }
            public TimeSpan ExecutionTimeout { get; set; }
        }
    }
}
