namespace Lykke.Service.QuotesHistory.Settings
{
    public class QuotesHistorySettings
    {
        public RabbitMqSettings RabbitMq { get; set; } = new RabbitMqSettings();
        public ConnectionStringsSettings ConnectionStrings { get; set; } = new ConnectionStringsSettings();
        public StorageRetrySettings StorageRetrySettings { get; set; } = new StorageRetrySettings();
    }
}