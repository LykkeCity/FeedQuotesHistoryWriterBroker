using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.QuotesHistory.Settings
{
    public class RabbitMqSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }
    }
}
