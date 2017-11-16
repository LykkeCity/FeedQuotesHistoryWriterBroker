using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
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
            ILog log)
            : base(nameof(QuotesBroker), (int)TimeSpan.FromMinutes(1).TotalMilliseconds, log)
        {
            _quotesManager = quotesManager;
            _log = log;

            var subscriberSettings = RabbitMqSubscriptionSettings
                .CreateForSubscriber(rabbitConnectionString, "quotefeed", "quoteshistory")
                .MakeDurable()
                .DelayTheRecconectionForA(delay: TimeSpan.FromSeconds(20));

            // HACK: Initially exchange was named not according to the pattern,
            // so preserving old name since dlx renaming is not trivial
            subscriberSettings.DeadLetterExchangeName = "lykke.quotefeed.quoteshistory.dlx";

            _subscriber = new RabbitMqSubscriber<Quote>(subscriberSettings,
                    new ResilientErrorHandlingStrategy(_log, subscriberSettings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        retryNum: 10,
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
            try
            {
                if (quote != null)
                {
                    await _quotesManager.ConsumeQuote(quote);
                }
                else
                {
                    await _log.WriteWarningAsync(nameof(QuotesBroker), nameof(HandleMessage), string.Empty,
                        "Received quote <NULL>.");
                }
            }
            catch
            {
                await _log.WriteWarningAsync(nameof(QuotesBroker), nameof(HandleMessage), quote.ToJson(), "Failed to process quote");
                throw;
            }
        }

        public override async Task Execute()
        {
            await _quotesManager.PersistQuotes();
        }
    }
}
