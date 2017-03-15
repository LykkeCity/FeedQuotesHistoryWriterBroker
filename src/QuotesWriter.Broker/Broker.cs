using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;

using Common;
using Common.Abstractions;
using Common.Log;
using Lykke.Domain.Prices.Model;
using Lykke.RabbitMqBroker.Subscriber;

using QuotesWriter.Broker.Serialization;
using QuotesWriter.Core;
using Lykke.Domain.Prices.Contracts;

namespace QuotesWriter.Broker
{
    internal sealed class Broker : TimerPeriod, IPersistent
    {
        private readonly static string COMPONENT_NAME = "FeedQuotesHistoryWriterBroker";
        private readonly static string PROCESS = "Broker";

        private RabbitMqSubscriber<Quote> subscriber;
        private Controller controller;
        private ILog logger;

        private bool isStarted = false;
        private bool isDisposed = false;

        private ILifetimeScope scope;
        public ILifetimeScope Scope
        {
            get
            {
                return this.scope;
            }
            set
            {
                this.scope = value;
                this.controller.Scope = scope;
            }
        }

        public Broker(
            RabbitMqSubscriber<Quote> subscriber,
            ILog logger)
            : base(COMPONENT_NAME, (int)TimeSpan.FromMinutes(1).TotalMilliseconds, logger)
        {
            this.logger = logger;
            this.subscriber = subscriber;

            subscriber
                  .SetMessageDeserializer(new MessageDeserializer())
                  .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                  .Subscribe(HandleMessage)
                  .SetLogger(logger);

            this.controller = new Controller(logger, COMPONENT_NAME);
        }

        private async Task HandleMessage(Quote quote)
        {
            if (quote != null)
            {
                await this.controller.ConsumeQuote(quote);
            }
            else
            {
                await this.logger.WriteWarningAsync(COMPONENT_NAME, string.Empty, string.Empty, "Received quote <NULL>.");
            }
        }

        public override async Task Execute()
        {
            await this.controller.Tick();
        }

        public async Task Save()
        {
            // Persist all remaining intervals
            await this.controller.Tick();
        }
    }
}
