using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.QuotesHistory.Core.Domain.Quotes;
using Microsoft.WindowsAzure.Storage.Table;
using MoreLinq;

namespace Lykke.Service.QuotesHistory.Repositories
{
    public class QuoteHistoryRepository : IQuoteHistoryRepository
    {
        private readonly INoSQLTableStorage<QuoteTableEntity> _tableStorage;

        public QuoteHistoryRepository(INoSQLTableStorage<QuoteTableEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        #region Public

        /// <inheritdoc/>
        public async Task<IEnumerable<IQuote>> GetQuotesAsync(string asset, bool isBuy, DateTime minute)
        {
            if (string.IsNullOrEmpty(asset)) { throw new ArgumentNullException(nameof(asset)); }

            var partitionKey = QuoteTableEntity.GeneratePartitionKey(asset, isBuy);
            var rowKey = QuoteTableEntity.GenerateRowKey(minute);

            var entity = await _tableStorage.GetDataAsync(partitionKey, rowKey);
            if (entity != null && entity.Quotes != null)
            {
                return entity.Quotes;
            }

            return new IQuote[0];
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<IQuote> Quotes, string ContinuationToken)> GetQuotesBulkAsync(string asset, bool isBuy, DateTime fromMoment, DateTime toMoment, string continuationToken = null)
        {
            var partitionKey = QuoteTableEntity.GeneratePartitionKey(asset, isBuy);
            var tableQuery = BuildTableQuery(partitionKey, fromMoment, toMoment);

#pragma warning disable IDE0042 // Deconstruct variable declaration
            var queryResult = await _tableStorage.GetDataWithContinuationTokenAsync(tableQuery, continuationToken);
#pragma warning restore IDE0042 // Deconstruct variable declaration

            if (!queryResult.Entities.Any())
                return (Quotes: new List<IQuote>(), null);

            var extensiveSet = queryResult.Entities.SelectMany(q => q.Quotes);

            return (
                Quotes: extensiveSet.Where(q => q.Timestamp > fromMoment && q.Timestamp <= toMoment), 
                ContinuationToken: queryResult.ContinuationToken
                );
        }

        /// <inheritdoc/>
        public async Task InsertOrMergeAsync(IQuote quote)
        {
            if (quote == null) { throw new ArgumentNullException(nameof(quote)); }

            await InsertOrMergeAsync(new IQuote[] { quote }, quote.AssetPair, quote.IsBuy);
        }

        /// <inheritdoc/>
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

        #endregion

        #region Private

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
            foreach (var batch in existingEntities.Batch(100))
            {
                await _tableStorage.InsertOrMergeBatchAsync(batch);
            }
        }

        private static TableQuery<QuoteTableEntity> BuildTableQuery(string partitionKey, DateTime fromMoment, DateTime toMoment)
        {
            var alignedFromMoment = QuoteTableEntity.GenerateRowKey(fromMoment);
            var alignedToMoment = QuoteTableEntity.GenerateRowKey(toMoment);

            var pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);

            var rkFromFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, alignedFromMoment);
            var rkToFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, alignedToMoment);
            var rkFinalFilter = TableQuery.CombineFilters(rkFromFilter, TableOperators.And, rkToFilter);

            var finalFilter = TableQuery.CombineFilters(pkFilter, TableOperators.And, rkFinalFilter);

            return new TableQuery<QuoteTableEntity>
            {
                FilterString = finalFilter
            };
        }

        #endregion
    }
}
