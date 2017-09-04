using System;
using Autofac;
using AzureStorage.Tables;
using Common;
using Common.Abstractions;
using Common.Log;
using FeedQuotesHistoryWriterBroker.Core;
using FeedQuotesHistoryWriterBroker.Repositories;
using FeedQuotesHistoryWriterBroker.Settings;
using Lykke.Domain.Prices.Model;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace FeedQuotesHistoryWriterBroker
{
    internal class Startup
    {
        private readonly AppSettings _settings;
        private Broker _broker;

        public static string ApplicationName => "FeedQuotesHistoryWriterBroker";

        public Startup(AppSettings settings)
        {
            _settings = settings;
        }

        public void ConfigureServices(ContainerBuilder builder, ILog log)
        {
            var mq = _settings.FeedQuotesHistoryWriterBroker.RabbitMq;

            var subscriberSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = mq.ConnectionString,
                QueueName = mq.QuoteFeedExchangeName + ".tickhistory",
                ExchangeName = mq.QuoteFeedExchangeName,
                DeadLetterExchangeName = mq.DeadLetterExchangeName,
                IsDurable = true,
                RoutingKey = ""
            };

            var subscriber = new RabbitMqSubscriber<Quote>(subscriberSettings, 
                new ResilientErrorHandlingStrategy(log, subscriberSettings,
                retryTimeout: TimeSpan.FromSeconds(10),
                next: new DeadQueueErrorHandlingStrategy(log, subscriberSettings)));

            _broker = new Broker(subscriber, log);

            builder.Register(c => new QuoteHistoryRepository(
                AzureTableStorage<QuoteTableEntity>.Create(
                        () => _settings.FeedQuotesHistoryWriterBroker.ConnectionStrings.HistoryConnectionString,
                        "QuotesHistory", log)
                )).As<IQuoteHistoryRepository>();

            builder.RegisterInstance(subscriber)
                .As<IStartable>()
                .As<IStopable>();

            builder.RegisterInstance(_broker)
                .As<IStartable>()
                .As<IStopable>()
                .As<IPersistent>();
        }

        public void Configure(ILifetimeScope scope)
        {
            _broker.Scope = scope;
        }
    }
}
