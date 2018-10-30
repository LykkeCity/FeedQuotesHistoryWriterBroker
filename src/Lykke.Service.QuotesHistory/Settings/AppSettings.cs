using JetBrains.Annotations;
using Lykke.Sdk.Settings;

namespace Lykke.Service.QuotesHistory.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public QuotesHistorySettings QuotesHistoryService { get; set; } = new QuotesHistorySettings();
    }
}
