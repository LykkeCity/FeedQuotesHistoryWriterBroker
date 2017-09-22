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

namespace FeedQuotesHistoryWriterBroker.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings _settings;
        private readonly ILog _log;

        public JobModule(AppSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings)
                .SingleInstance();

            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            var storage = new AzureTableStorage<QuoteTableEntity>(
                _settings.FeedQuotesHistoryWriterBroker.ConnectionStrings.HistoryConnectionString,
                "QuotesHistory", _log);

            builder.Register(c => new QuoteHistoryRepository(                
                new RetryOnFailureAzureTableStorageDecorator<QuoteTableEntity>(
                    storage, 
                    _settings.FeedQuotesHistoryWriterBroker.StorageRetrySettings.OnModificationsRetryCount,
                    _settings.FeedQuotesHistoryWriterBroker.StorageRetrySettings.OnGettingRetryCount,
                    _settings.FeedQuotesHistoryWriterBroker.StorageRetrySettings.RetryDelay)
            )).As<IQuoteHistoryRepository>();

            builder.RegisterType<QuotesManager>()
                .As<IQuotesManager>();

            builder.RegisterType<QuotesBroker>()
                .As<IStartable>()
                .As<IQuotesBroker>()
                .WithParameter(TypedParameter.From(_settings.FeedQuotesHistoryWriterBroker.RabbitMq))
                .AutoActivate()
                .SingleInstance();
        }
    }
}