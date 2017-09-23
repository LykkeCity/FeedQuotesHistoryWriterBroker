using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using FeedQuotesHistoryWriterBroker.Core.Services.Quotes;
using FeedQuotesHistoryWriterBroker.Services.Serialization;
using Lykke.Domain.Prices.Model;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace FeedQuotesHistoryWriterBroker.Services.Quotes
{
    public sealed class QuotesBroker : TimerPeriod, IQuotesBroker
    {
        private readonly IQuotesManager _quotesManager;
        private readonly RabbitMqSubscriber<Quote> _subscriber;
        private readonly ILog _log;

        public QuotesBroker(
            IQuotesManager quotesManager,
            string rabbitConnectionString,
            ILog log)
            : base(nameof(QuotesBroker), (int)TimeSpan.FromMinutes(1).TotalMilliseconds, log)
        {
            _quotesManager = quotesManager;
            _log = log;

            var subscriberSettings = RabbitMqSubscriptionSettings
                .CreateForSubscriber(rabbitConnectionString, "quotefeed", "quoteshistory")
                .MakeDurable()
                .DelayTheRecconectionForA(delay: TimeSpan.FromSeconds(20));

            _subscriber = new RabbitMqSubscriber<Quote>(subscriberSettings,
                    new ResilientErrorHandlingStrategy(_log, subscriberSettings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_log, subscriberSettings)))
                .SetMessageDeserializer(new MessageDeserializer())
                .Subscribe(HandleMessage)
                .SetMessageReadStrategy(new MessageReadQueueStrategy())
                .CreateDefaultBinding()
                .SetLogger(log)
                .Start();
        }

        public override void Stop()
        {
            _subscriber?.Stop();

            base.Stop();

            // Persist all remaining intervals
            _quotesManager.PersistQuotes().Wait();
        }

        private async Task HandleMessage(Quote quote)
        {
            if (quote != null)
            {
                await _quotesManager.ConsumeQuote(quote);
            }
            else
            {
                await _log.WriteWarningAsync(nameof(QuotesBroker), nameof(HandleMessage), string.Empty, "Received quote <NULL>.");
            }
        }

        public override async Task Execute()
        {
            await _quotesManager.PersistQuotes();
        }
    }
}
