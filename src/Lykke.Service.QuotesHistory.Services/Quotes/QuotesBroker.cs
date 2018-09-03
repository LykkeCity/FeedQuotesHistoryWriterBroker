using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Domain.Prices.Model;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.QuotesHistory.Core.Services.Quotes;
using Lykke.Service.QuotesHistory.Services.Serialization;

namespace Lykke.Service.QuotesHistory.Services.Quotes
{
    public sealed class QuotesBroker : TimerPeriod, IQuotesBroker
    {
        private readonly IQuotesManager _quotesManager;
        private readonly RabbitMqSubscriber<Quote> _subscriber;
        private readonly ILog _log;

        public QuotesBroker(
            IQuotesManager quotesManager,
            string rabbitConnectionString,
            ILogFactory logFactory)
            : base(TimeSpan.FromMinutes(1), logFactory)
        {
            _quotesManager = quotesManager;
            _log = logFactory.CreateLog(this);

            var subscriberSettings = RabbitMqSubscriptionSettings
                .CreateForSubscriber(rabbitConnectionString, "quotefeed", "quoteshistory")
                .MakeDurable()
                .DelayTheRecconectionForA(delay: TimeSpan.FromSeconds(20));

            // HACK: Initially exchange was named not according to the pattern,
            // so preserving old name since dlx renaming is not trivial
            subscriberSettings.DeadLetterExchangeName = "lykke.quotefeed.quoteshistory.dlx";

            _subscriber = new RabbitMqSubscriber<Quote>(
                logFactory, 
                subscriberSettings,
                new ResilientErrorHandlingStrategy(
                    logFactory, 
                    subscriberSettings,
                    retryTimeout: TimeSpan.FromSeconds(10),
                    retryNum: 10,
                    next: new DeadQueueErrorHandlingStrategy(logFactory, subscriberSettings)
                    )
                 )
                .SetMessageDeserializer(new MessageDeserializer())
                .Subscribe(HandleMessage)
                .SetMessageReadStrategy(new MessageReadQueueStrategy())
                .CreateDefaultBinding()
                .Start();
        }

        public override void Stop()
        {
            _subscriber?.Stop();

            base.Stop();

            // Persist all remaining intervals
            _quotesManager.PersistQuotes().Wait();
        }

        private Task HandleMessage(Quote quote)
        {
            try
            {
                if (quote != null)
                {
                    _quotesManager.ConsumeQuote(quote);
                }
                else
                {
                    _log.Warning("Received quote <NULL>.");
                }

                return Task.CompletedTask; // For to match requirements of MQ Subscriber.
            }
            catch
            {
                _log.Warning("Failed to process quote", context: quote.ToJson());
                throw;
            }
        }

        public override async Task Execute()
        {
            await _quotesManager.PersistQuotes();
        }
    }
}
