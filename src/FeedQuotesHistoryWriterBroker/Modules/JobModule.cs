using Autofac;
using AzureStorage.Tables;
using AzureStorage.Tables.Decorators;
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

            var storage = AzureTableStorage<QuoteTableEntity>.Create(
                _settings.ConnectionString(x => x.ConnectionStrings.HistoryConnectionString), "QuotesHistory", _log);

            builder.Register(c => new QuoteHistoryRepository(                
                new RetryOnFailureAzureTableStorageDecorator<QuoteTableEntity>(
                    storage, 
                    _settings.CurrentValue.StorageRetrySettings.OnModificationsRetryCount,
                    _settings.CurrentValue.StorageRetrySettings.OnGettingRetryCount,
                    _settings.CurrentValue.StorageRetrySettings.RetryDelay)
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