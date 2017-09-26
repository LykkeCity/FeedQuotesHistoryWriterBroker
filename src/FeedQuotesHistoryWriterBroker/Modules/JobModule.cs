using Autofac;
using AzureStorage.Tables;
using Common.Log;
using FeedQuotesHistoryWriterBroker.Core.Domain.Quotes;
using FeedQuotesHistoryWriterBroker.Core.Services;
using FeedQuotesHistoryWriterBroker.Core.Services.Quotes;
using FeedQuotesHistoryWriterBroker.Core.Settings;
using FeedQuotesHistoryWriterBroker.Repositories;
using FeedQuotesHistoryWriterBroker.Services;
using FeedQuotesHistoryWriterBroker.Services.Quotes;
using Lykke.SettingsReader;

namespace FeedQuotesHistoryWriterBroker.Modules
{
    public class JobModule : Module
    {
        private readonly IReloadingManager<AppSettings.FeedQuotesHistoryWriterBrokerSettings> _settings;
        private readonly ILog _log;

        public JobModule(IReloadingManager<AppSettings.FeedQuotesHistoryWriterBrokerSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.Register(c => new QuoteHistoryRepository(
                AzureTableStorage<QuoteTableEntity>.Create(
                    _settings.ConnectionString(x => x.ConnectionStrings.HistoryConnectionString), "QuotesHistory", _log,
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