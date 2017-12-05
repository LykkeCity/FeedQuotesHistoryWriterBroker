using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.QuotesHistory.Core.Domain.Quotes
{
    public interface IQuoteHistoryRepository
    {
        /// <summary>
        /// Returns quotes for the specified minute.
        /// </summary>
        /// <param name="minute">Seconds and milliseconds are not significant.</param>
        Task<IEnumerable<IQuote>> GetQuotesAsync(string assetPairs, bool isBuy, DateTime minute);


        Task<IReadOnlyCollection<IQuote>> GetQuotesAsync(DateTime from, DateTime to, IEnumerable<string> assetPairs, CancellationToken cancellationToken);

        /// <summary>
        /// Inserts or merges specified quote to the azure table
        /// </summary>
        /// <param name="quote"></param>
        /// <returns></returns>
        Task InsertOrMergeAsync(IQuote quote);

        /// <summary>
        /// Filters <paramref name="quotes"/> with specified <paramref name="assetPair"/> and isBuy sign and inserts them (or merges) to the azure table.
        /// </summary>
        /// <param name="quotes">Collection of <paramref name="quotes"/> to insert/merge</param>
        /// <param name="assetPair">Asset pair to filter <paramref name="quotes"/></param>
        /// <param name="isBuy">IsBuy sign to filter <paramref name="quotes"/></param>
        Task InsertOrMergeAsync(IReadOnlyCollection<IQuote> quotes, string assetPair, bool isBuy);
    }
}
