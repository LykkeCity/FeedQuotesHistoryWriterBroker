using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using FeedQuotesHistoryWriterBroker.Core.Domain.Quotes;
using FeedQuotesHistoryWriterBroker.Core.Services.Quotes;
using Lykke.Domain.Prices.Model;

namespace FeedQuotesHistoryWriterBroker.Services.Quotes
{
    public class QuotesManager : IQuotesManager
    {
        private const int ChunkSize = 100;

        private readonly IQuoteHistoryRepository _repo;
        private readonly ILog _log;
        private readonly ConcurrentQueue<Quote> _quotesQueue = new ConcurrentQueue<Quote>();

        /// <summary>Last time service logs were made</summary>
        private DateTime _lastServiceLogTime = DateTime.MinValue;
        /// <summary>Average write duration</summary>
        private TimeSpan _avgWriteSpan = TimeSpan.Zero;

        public QuotesManager(IQuoteHistoryRepository repo, ILog log)
        {
            _repo = repo;
            _log = log;
        }

        public async Task ConsumeQuote(Quote quote)
        {
            // Validate
            // 
            var validationErrors = Validate(quote);
            if (validationErrors.Count > 0)
            {
                foreach (var error in validationErrors)
                {
                    await _log.WriteErrorAsync(nameof(QuotesManager), nameof(ConsumeQuote), string.Empty, new ArgumentException("Received invalid quote. " + error));
                }
                return; // Skipping invalid quotes
            }

            // Add quote to the processing queue
            _quotesQueue.Enqueue(quote);
        }

        public async Task PersistQuotes()
        {
            // Get a snapshot of quotes collection and process it
            //
            var countQuotes = _quotesQueue.Count;
            var unprocessedQuotes = new List<Quote>(countQuotes);

            for (var i = 0; i < countQuotes; i++)
            {
                if (_quotesQueue.TryDequeue(out Quote quote))
                {
                    unprocessedQuotes.Add(quote);
                }
                else
                {
                    break;
                }
            }

            await ProcessQuotes(unprocessedQuotes);
        }

        private async Task ProcessQuotes(IReadOnlyCollection<Quote> unprocessedQuotes)
        {
            if (unprocessedQuotes.Count == 0)
            {
                return;
            }

            var watch = new Stopwatch();
            watch.Start();
            
            // Group quotes by asset
            var assetGroups = unprocessedQuotes
                .GroupBy(q => q.AssetPair)
                .Select(assetGroup => assetGroup);

            // Write to storage simultaneously with maximum tasks number
            foreach (var collection in assetGroups.ToPieces(ChunkSize))
            {
                var tasks = collection
                    .Select(group => ProcessQuotesForAsset(group.ToArray(), group.Key))
                    .ToList();

                await Task.WhenAll(tasks);
            }
            
            // Update average write time and log service information
            //
            watch.Stop();
            _avgWriteSpan = new TimeSpan((_avgWriteSpan.Ticks + watch.Elapsed.Ticks) / 2);
            if (DateTime.UtcNow - _lastServiceLogTime > TimeSpan.FromHours(1))
            {
                await _log.WriteInfoAsync(nameof(QuotesManager), nameof(ProcessQuotes), string.Empty, $"Average write time: {_avgWriteSpan}");
                _lastServiceLogTime = DateTime.UtcNow;
            }
        }

        private async Task ProcessQuotesForAsset(IReadOnlyCollection<Quote> quotes, string asset)
        {
            var buyQuotes = quotes.Where(q => q.IsBuy);
            var sellQuotes = quotes.Where(q => !q.IsBuy);

            await _repo.InsertOrMergeAsync(buyQuotes.ToArray(), asset, true);
            await _repo.InsertOrMergeAsync(sellQuotes.ToArray(), asset, false);
        }

        private static ICollection<string> Validate(Quote quote)
        {
            var errors = new List<string>();

            if (quote == null)
            {
                errors.Add("Argument 'Order' is null.");
            }
            if (quote != null && string.IsNullOrEmpty(quote.AssetPair))
            {
                errors.Add(string.Format("Invalid 'AssetPair': '{0}'", quote.AssetPair ?? ""));
            }
            if (quote != null && (quote.Timestamp == DateTime.MinValue || quote.Timestamp == DateTime.MaxValue))
            {
                errors.Add(string.Format("Invalid 'Timestamp' range: '{0}'", quote.Timestamp));
            }
            if (quote != null && quote.Timestamp.Kind != DateTimeKind.Utc)
            {
                errors.Add(string.Format("Invalid 'Timestamp' Kind (UTC is required): '{0}'", quote.Timestamp));
            }

            return errors;
        }
    }
}
