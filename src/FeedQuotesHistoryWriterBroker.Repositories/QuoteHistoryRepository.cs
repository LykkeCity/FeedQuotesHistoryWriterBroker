using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using FeedQuotesHistoryWriterBroker.Core;
using FeedQuotesHistoryWriterBroker.Core.Domain.Quotes;
using Lykke.Domain.Prices.Contracts;

namespace FeedQuotesHistoryWriterBroker.Repositories
{
    public class QuoteHistoryRepository : IQuoteHistoryRepository
    {
        private readonly INoSQLTableStorage<QuoteTableEntity> _tableStorage;

        public QuoteHistoryRepository(INoSQLTableStorage<QuoteTableEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<IQuote>> GetQuotesAsync(string asset, bool isBuy, DateTime minute)
        {
            if (string.IsNullOrEmpty(asset)) { throw new ArgumentNullException(nameof(asset)); }

            string partitionKey = QuoteTableEntity.GeneratePartitionKey(asset, isBuy);
            string rowKey = QuoteTableEntity.GenerateRowKey(minute);

            var entity = await _tableStorage.GetDataAsync(partitionKey, rowKey);
            if (entity != null && entity.Quotes != null)
            {
                return entity.Quotes;
            }

            return new IQuote[0];
        }

        public async Task InsertOrMergeAsync(IQuote quote)
        {
            if (quote == null) { throw new ArgumentNullException(nameof(quote)); }

            await InsertOrMergeAsync(new IQuote[] { quote }, quote.AssetPair, quote.IsBuy);
        }

        public async Task InsertOrMergeAsync(IReadOnlyCollection<IQuote> quotes, string asset, bool isBuy)
        {
            if (quotes == null) { throw new ArgumentNullException(nameof(quotes)); }
            if (string.IsNullOrEmpty(asset)) { throw new ArgumentNullException(nameof(asset)); }

            // Select only quotes with specified asset and buy sign
            quotes = quotes
                .Where(q => q.AssetPair == asset && q.IsBuy == isBuy)
                .ToArray();
            if (!quotes.Any())
            {
                return;
            }

            string partitionKey = QuoteTableEntity.GeneratePartitionKey(asset, isBuy);

            var newEntities = new List<QuoteTableEntity>();

            // Group quotes by row keys
            var groups = quotes
                .GroupBy(candle => candle.RowKey())
                .ToArray();
            foreach (var group in groups)
            {
                var e = new QuoteTableEntity(partitionKey, group.Key);
                e.Quotes.AddRange(group.Select(q => q.ToItem()));
                newEntities.Add(e);
            }

            await InsertOrMergeAsync(newEntities, partitionKey, groups.Select(g => g.Key));
        }

        /// <summary>
        /// Inserts or meges entities with the same partition key
        /// </summary>
        private async Task InsertOrMergeAsync(List<QuoteTableEntity> entitites, string partitionKey, IEnumerable<string> rowKeys)
        {
            // 1. Read all { pkey, rowkey } rows
            //
            var existingEntities = (await _tableStorage.GetDataAsync(partitionKey, rowKeys)).ToList();

            // 2. Update rows (merge entities)
            //
            foreach(var entity in entitites)
            {
                var existingEntity = existingEntities.Where(e => e.PartitionKey == entity.PartitionKey && e.RowKey == entity.RowKey).FirstOrDefault();
                if (existingEntity == null)
                {
                    existingEntities.Add(entity);
                }
                else
                {
                    existingEntity.Merge(entity);
                }
            }

            // 3. Write rows in batch
            foreach (var batch in existingEntities.ToPieces(100))
            {
                await _tableStorage.InsertOrMergeBatchAsync(batch);
            }
        }
    }
}
