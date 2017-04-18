using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json;

using AzureStorage.Tables;
using AzureRepositories.Quotes;
using Common;
using Common.Abstractions;
using Common.Log;
using Lykke.Domain.Prices.Model;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using QuotesWriter.Core;

namespace QuotesWriter.Broker
{
    internal class Startup
    {
        private AppSettings settings;
        private Broker broker = null;

        public static string ApplicationName { get { return "FeedQuotesHistoryWriterBroker"; } }

        public Startup(AppSettings settings, ILog log)
        {
            this.settings = settings;
        }

        public void ConfigureServices(ContainerBuilder builder, ILog log)
        {
            var mq = settings.FeedQuotesHistoryWriterBroker.RabbitMq;
            
            RabbitMqSubscriberSettings subscriberSettings = new RabbitMqSubscriberSettings()
            {
                ConnectionString = $"amqp://{mq.Username}:{mq.Password}@{mq.Host}:{mq.Port}",
                QueueName = mq.QuoteFeed + ".tickhistory",
                ExchangeName = mq.QuoteFeed,
                IsDurable = true
            };

            var subscriber = new RabbitMqSubscriber<Quote>(subscriberSettings);
            this.broker = new Broker(subscriber, log);

            builder.Register(c => new QuoteHistoryRepository(
                new AzureTableStorage<QuoteTableEntity>(
                        settings.FeedQuotesHistoryWriterBroker.ConnectionStrings.HistoryConnectionString,
                        "QuotesHistory", log)
                )).As<IQuoteHistoryRepository>();

            builder.RegisterInstance(subscriber)
                .As<IStartable>()
                .As<IStopable>();

            builder.RegisterInstance(this.broker)
                .As<IStartable>()
                .As<IStopable>()
                .As<IPersistent>();
        }

        public void Configure(ILifetimeScope scope)
        {
            this.broker.Scope = scope;
        }
    }
}
