using System;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.QuotesHistory.Repositories
{
    public static class QuoteExtensions
    {
        public static QuoteItem ToItem(this IQuote q)
        {
            if (q == null) { throw new ArgumentNullException(nameof(q)); }

            return new QuoteItem()
            {
                AssetPair = q.AssetPair,
                IsBuy = q.IsBuy,
                Price = q.Price,
                Timestamp = q.Timestamp
            };
        }

        public static string PartitionKey(this IQuote quote)
        {
            if (quote == null) { throw new ArgumentNullException(nameof(quote)); }

            return QuoteTableEntity.GeneratePartitionKey(quote.AssetPair, quote.IsBuy);
        }

        public static string RowKey(this IQuote quote)
        {
            if (quote == null)
            {
                throw new ArgumentNullException(nameof(quote));
            }
            if (quote.Timestamp.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Quote timestamp should be UTC", nameof(quote));
            }

            return QuoteTableEntity.GenerateRowKey(quote.Timestamp);
        }
    }

    public static class QuoteTableEntityExtensions
    {
        /// <summary>
        /// Merge with data from another entity
        /// </summary>
        public static void Merge(this QuoteTableEntity entity, QuoteTableEntity patch)
        {
            if (entity == null) { throw new ArgumentNullException(nameof(entity)); }
            if (patch == null) { throw new ArgumentNullException(nameof(patch)); }

            entity.Quotes.AddRange(patch.Quotes);
        }
    }
}
