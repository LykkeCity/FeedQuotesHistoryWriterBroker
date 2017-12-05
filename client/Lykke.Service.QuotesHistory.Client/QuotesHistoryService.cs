using System;

namespace Lykke.Service.QuotesHistory.Client.AutorestClient
{
    public partial class QuotesHistoryService
    {
        partial void CustomInitialize()
        {
            HttpClient.Timeout = TimeSpan.FromMinutes(30);
        }
    }
}
