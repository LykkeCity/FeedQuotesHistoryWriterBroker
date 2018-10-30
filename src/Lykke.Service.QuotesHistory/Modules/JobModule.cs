using Autofac;
using AzureStorage.Tables;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Sdk.Health;
using Lykke.Service.QuotesHistory.Core.Domain.Quotes;
using Lykke.Service.QuotesHistory.Core.Services.Quotes;
using Lykke.Service.QuotesHistory.Repositories;
using Lykke.Service.QuotesHistory.Services.Quotes;
using Lykke.Service.QuotesHistory.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.QuotesHistory.Modules
{
    [UsedImplicitly]
    public class JobModule : Module
    {
        private readonly IReloadingManager<QuotesHistorySettings> _settings;

        public JobModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings.Nested(x => x.QuotesHistoryService);
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.Register(c => new QuoteHistoryRepository(
                AzureTableStorage<QuoteTableEntity>.Create(
                    _settings.ConnectionString(x => x.ConnectionStrings.HistoryConnectionString), "QuotesHistory", c.Resolve<ILogFactory>(),
                    onModificationRetryCount:_settings.CurrentValue.StorageRetrySettings.OnModificationsRetryCount,
                    onGettingRetryCount:_settings.CurrentValue.StorageRetrySettings.OnGettingRetryCount,
                    retryDelay:_settings.CurrentValue.StorageRetrySettings.RetryDelay)
            )).As<IQuoteHistoryRepository>();

            builder.RegisterType<QuotesManager>()
                .As<IQuotesManager>();

            builder.RegisterType<QuotesBroker>()
                .As<IStartable>()
                .As<IQuotesBroker>()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.RabbitMq.ConnectionString))
                .AutoActivate()
                .SingleInstance();
        }
    }
}
