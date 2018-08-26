using System;
using System.Collections.Generic;
using System.Linq;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using QuotesHistoryCorrectionTool.Infrastructure;

// ReSharper disable once CheckNamespace
namespace QuotesHistoryCorrectionTool.Repository
{
    public class QuotesHistoryRepository : IQuotesHistoryRepository
    {
        private readonly ILog _log;
        private readonly INoSQLTableStorage<QuotesHistoryEntity> _sourceStorage;
        private readonly INoSQLTableStorage<QuotesHistoryEntity> _destStorage;

        private List<QuotesHistoryEntity> _queueToInsert;
        private readonly int _persistenceQueueMaxSize;

        private readonly DateTime _startTime;
        private int _totalInsertedCount;

        public bool IoStateFailed { get; private set; }

        public QuotesHistoryRepository(
            ILog log, 
            IReloadingManager<string> azureStorageSourceConnString,
            IReloadingManager<string> azureStorageDestinationConnString,
            DataCorrectionSettings correctionSettings)
        {
            _log = log.CreateComponentScope(nameof(QuotesHistoryRepository)) ?? throw new ArgumentNullException(nameof(log));

            if (string.IsNullOrEmpty(azureStorageSourceConnString?.CurrentValue))
                throw new ArgumentNullException(nameof(azureStorageSourceConnString));

            if (string.IsNullOrEmpty(azureStorageDestinationConnString?.CurrentValue))
                throw new ArgumentNullException(nameof(azureStorageDestinationConnString));

            if (string.IsNullOrWhiteSpace(correctionSettings.SourceTableName))
                throw new ArgumentNullException(nameof(correctionSettings.SourceTableName));
            if (string.IsNullOrWhiteSpace(correctionSettings.DestinationTableName))
                throw new ArgumentNullException(nameof(correctionSettings.DestinationTableName));

            if (azureStorageSourceConnString == azureStorageDestinationConnString &&
                correctionSettings.SourceTableName == correctionSettings.DestinationTableName)
                throw new InvalidOperationException("The source and the destination data storages shall not be the same. Check up the settings.");

            _sourceStorage = AzureTableStorage<QuotesHistoryEntity>.Create(
                azureStorageSourceConnString,
                correctionSettings.SourceTableName,
                _log,
                maxExecutionTimeout: TimeSpan.FromMinutes(1),
                retryDelay: TimeSpan.FromSeconds(1)); // createTableAutomatically = true by default

            _destStorage = AzureTableStorage<QuotesHistoryEntity>.Create(
                azureStorageDestinationConnString,
                correctionSettings.DestinationTableName,
                _log,
                maxExecutionTimeout: TimeSpan.FromMinutes(1),
                retryDelay: TimeSpan.FromSeconds(1)); // createTableAutomatically = true by default

            _queueToInsert = new List<QuotesHistoryEntity>();

            _persistenceQueueMaxSize = correctionSettings.PersistenceQueueMaxSize;

            _startTime = DateTime.UtcNow;
        }

        public (IEnumerable<QuotesHistoryEntity> data, string token) GetDataAsync(int batchSize, string continuationToken)
        {
            return _sourceStorage.GetDataWithContinuationTokenAsync(batchSize, continuationToken).GetAwaiter().GetResult();
        }

        public bool InsertEntity(QuotesHistoryEntity entity)
        {
            if (IoStateFailed)
                return false;

            try
            {
                _queueToInsert.Add(entity);

                if (_queueToInsert.Count >= _persistenceQueueMaxSize)
                    Flush();

                return true;
            }
            catch (Exception ex)
            {
                IoStateFailed = true;
                _log.WriteErrorAsync(nameof(InsertEntity), null, ex).Wait();
                return false;
            }
        }

        public void Flush()
        {
            if (IoStateFailed)
                throw new InvalidOperationException("Can't flush a data to the repository in failed I/O state.");

            try
            {
                foreach (var chunk in _queueToInsert.GroupBy(item => item.PartitionKey))
                {
                    _destStorage.InsertAsync(chunk).Wait();

                    var chunkSize = chunk.Count();
                    _totalInsertedCount += chunkSize;

                    _log.WriteInfoAsync(nameof(Flush),
                        string.Empty,
                        $"\nBatch of {chunkSize} records stored. The first record was PK = {chunk.First().PartitionKey} & RK = {chunk.First().RowKey}, " +
                        $"the last record was PK = {chunk.Last().PartitionKey} & RK = {chunk.Last().RowKey}." +
                        $"\nThe whole process was started at {_startTime:G}, time elapsed is {DateTime.UtcNow - _startTime}. Totally inserted {_totalInsertedCount} items.",
                        operation: LogExtension.LoggingOperation.Insert,
                        useThrottling: true).Wait();
                }

                _queueToInsert = new List<QuotesHistoryEntity>();
            }
            catch
            {
                IoStateFailed = true;
                throw;
            }
        }
    }
}
