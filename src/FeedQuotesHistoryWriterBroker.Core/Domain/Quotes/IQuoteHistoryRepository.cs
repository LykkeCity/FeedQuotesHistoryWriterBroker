using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices.Contracts;

namespace FeedQuotesHistoryWriterBroker.Core.Domain.Quotes
{
    public interface IQuoteHistoryRepository
    {
        /// <summary>
        /// Returns quotes for the specified minute.
        /// </summary>
        /// <param name="minute">Seconds and milliseconds are not significant.</param>
        Task<IEnumerable<IQuote>> GetQuotesAsync(string asset, bool isBuy, DateTime minute);

        /// <summary>
        /// Inserts or merges specified quote to the azure table
        /// </summary>
        /// <param name="quote"></param>
        /// <returns></returns>
        Task InsertOrMergeAsync(IQuote quote);

        /// <summary>
        /// Filters quotes with specified asset and isBuy sign and inserts them (or merges) to the azure table.
        /// </summary>
        /// <param name="quotes">Collection of quotes to insert/merge</param>
        /// <param name="asset">Asset pair to filter quotes</param>
        /// <param name="isBuy">IsBuy sign to filter quotes</param>
        Task InsertOrMergeAsync(IReadOnlyCollection<IQuote> quotes, string asset, bool isBuy);
    }
}
