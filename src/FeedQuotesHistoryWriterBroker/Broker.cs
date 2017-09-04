using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Abstractions;
using Common.Log;
using FeedQuotesHistoryWriterBroker.Core;
using FeedQuotesHistoryWriterBroker.Serialization;
using Lykke.Domain.Prices.Model;
using Lykke.RabbitMqBroker.Subscriber;

namespace FeedQuotesHistoryWriterBroker
{
    internal sealed class Broker : TimerPeriod, IPersistent
    {
        private static readonly string COMPONENT_NAME = "FeedQuotesHistoryWriterBroker";

        private readonly Controller _controller;
        private readonly ILog _logger;

        private ILifetimeScope _scope;
        public ILifetimeScope Scope
        {
            get
            {
                return _scope;
            }
            set
            {
                _scope = value;
                _controller.Scope = _scope;
            }
        }

        public Broker(
            RabbitMqSubscriber<Quote> subscriber,
            ILog logger)
            : base(COMPONENT_NAME, (int)TimeSpan.FromMinutes(1).TotalMilliseconds, logger)
        {
            _logger = logger;

            // Using default message reader strategy
            subscriber
                .SetMessageDeserializer(new MessageDeserializer())
                .Subscribe(HandleMessage)
                .SetMessageReadStrategy(new MessageReadQueueStrategy())
                .CreateDefaultBinding()
                .SetLogger(logger)
                .Start();

            _controller = new Controller(logger, COMPONENT_NAME);
        }

        private async Task HandleMessage(Quote quote)
        {
            if (quote != null)
            {
                await _controller.ConsumeQuote(quote);
            }
            else
            {
                await _logger.WriteWarningAsync(COMPONENT_NAME, string.Empty, string.Empty, "Received quote <NULL>.");
            }
        }

        public override async Task Execute()
        {
            await _controller.Tick();
        }

        public async Task Save()
        {
            // Persist all remaining intervals
            await _controller.Tick();
        }
    }
}
