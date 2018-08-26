using System;
using Lykke.SettingsReader.Attributes;

// ReSharper disable once CheckNamespace
namespace QuotesHistoryCorrectionTool.Infrastructure
{
    public class AppSettings
    {
        public ConnectionStringsSettings ConnectionStrings { get; set; }
        public SlackNotificationSettings SlackNotification { get; set; }
        public DataCorrectionSettings DataCorrection { get; set; }
    }

    public class ConnectionStringsSettings
    {
        [AzureTableCheck]
        public string SourceHistoryConnectionString { get; set; }
        [AzureTableCheck]
        public string DestinationHistoryConnectionString { get; set; }
        [AzureTableCheck]
        public string LogsConnectionString { get; set; }
    }

    public class SlackNotificationSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
        public int ThrottlingLimitSeconds { get; set; }
    }

    public class AzureQueueSettings
    {
        [AzureQueueCheck]
        public string ConnectionString { get; set; }
        public string QueueName { get; set; }
    }

    public class DataCorrectionSettings
    {
        public string SourceTableName { get; set; }
        public string DestinationTableName { get; set; }
        public int QueryBatchSize { get; set; }
        public int PersistenceQueueMaxSize { get; set; }
        public TimeSpan InfoMessagesTimeout { get; set; }
    }
}
