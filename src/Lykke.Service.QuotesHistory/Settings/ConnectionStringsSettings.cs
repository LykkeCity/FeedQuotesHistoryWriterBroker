using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.QuotesHistory.Settings
{
    public class ConnectionStringsSettings
    {
        [AzureTableCheck]
        public string HistoryConnectionString { get; set; }
        [AzureTableCheck]
        public string LogsConnectionString { get; set; }
    }
}
