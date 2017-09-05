namespace FeedQuotesHistoryWriterBroker.Core.Settings
{
    public class AppSettings
    {
        public SlackNotificationsSettings SlackNotifications { get; set; } = new SlackNotificationsSettings();
        public FeedQuotesHistoryWriterBrokerSettings FeedQuotesHistoryWriterBroker { get; set; } = new FeedQuotesHistoryWriterBrokerSettings();

        public class FeedQuotesHistoryWriterBrokerSettings
        {
            public RabbitMqSettings RabbitMq { get; set; } = new RabbitMqSettings();
            public ConnectionStringsSettings ConnectionStrings { get; set; } = new ConnectionStringsSettings();
        }

        public class RabbitMqSettings
        {
            public string ConnectionString { get; set; }
            public string QuoteFeedExchangeName { get; set; }
            public string DeadLetterExchangeName { get; set; }
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
    }
}
