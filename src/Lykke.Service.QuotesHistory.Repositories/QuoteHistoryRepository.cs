using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.QuotesHistory.Core.Domain.Quotes;
using Microsoft.WindowsAzure.Storage.Table;
using MoreLinq;

namespace Lykke.Service.QuotesHistory.Repositories
{
    public sealed class QuoteHistoryRepository : IQuoteHistoryRepository
    {
        private readonly INoSQLTableStorage<QuoteTableEntity> _tableStorage;

        public QuoteHistoryRepository(INoSQLTableStorage<QuoteTableEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<IQuote>> GetQuotesAsync(string asset, bool isBuy, DateTime minute)
        {
            if (string.IsNullOrEmpty(asset)) { throw new ArgumentNullException(nameof(asset)); }

            var partitionKey = QuoteTableEntity.GeneratePartitionKey(asset, isBuy);
            var rowKey = QuoteTableEntity.GenerateRowKey(minute);

            var entity = await _tableStorage.GetDataAsync(partitionKey, rowKey);
            if (entity?.Quotes != null)
            {
                return entity.Quotes;
            }

            return new IQuote[0];
        }

        public async Task<IReadOnlyCollection<IQuote>> GetQuotesAsync(DateTime from, DateTime to, IEnumerable<string> assets, CancellationToken cancellationToken)
        {

            var fromKey = QuoteTableEntity.GenerateRowKey(from);
            var toKey = QuoteTableEntity.GenerateRowKey(to);

            var queries = assets.Select(a => new[]
            {
                QuoteTableEntity.GeneratePartitionKey(a, false),
                QuoteTableEntity.GeneratePartitionKey(a, true)
            })
            .SelectMany(partKeys =>
                {
                    return partKeys.Select(pk =>
                     {
                         var assetFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pk);
                         var dateFilter = TableQuery.CombineFilters(
                             TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, fromKey),
                             TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, toKey));
                         return new TableQuery<QuoteTableEntity>().Where(TableQuery.CombineFilters(assetFilter, TableOperators.And, dateFilter));
                     });
                });


            var result = new ConcurrentBag<IQuote>();

            const int maxParallelism = 10;
            foreach (var batch in queries.Batch(maxParallelism))
            {
                await Task.WhenAll(batch.Select(query => _tableStorage.ExecuteAsync(query, entities =>
                {
                    if (entities == null)
                    {
                        return;
                    }
                    foreach (var quote in entities.SelectMany(e => e.Quotes))
                    {
                        result.Add(quote);
                    }
                }, () => !cancellationToken.IsCancellationRequested)));
            }

            return result;
        }

        public async Task InsertOrMergeAsync(IQuote quote)
        {
            if (quote == null) { throw new ArgumentNullException(nameof(quote)); }

            await InsertOrMergeAsync(new[] { quote }, quote.AssetPair, quote.IsBuy);
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

            var partitionKey = QuoteTableEntity.GeneratePartitionKey(asset, isBuy);

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
        /// Inserts or merges entities with the same partition key
        /// </summary>
        private async Task InsertOrMergeAsync(List<QuoteTableEntity> entitites, string partitionKey, IEnumerable<string> rowKeys)
        {
            // 1. Read all { pkey, rowkey } rows
            //
            var existingEntities = (await _tableStorage.GetDataAsync(partitionKey, rowKeys)).ToList();

            // 2. Update rows (merge entities)
            //
            foreach (var entity in entitites)
            {
                var existingEntity = existingEntities.FirstOrDefault(e => e.PartitionKey == entity.PartitionKey && e.RowKey == entity.RowKey);
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
