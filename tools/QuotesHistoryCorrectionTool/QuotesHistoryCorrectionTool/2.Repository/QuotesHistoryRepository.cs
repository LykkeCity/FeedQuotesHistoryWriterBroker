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
        private List<QuotesHistoryEntity> _queueToDelete;
        private readonly int _removalQueueMaxSize;

        private readonly DateTime _startTime;
        private int _totalInsertedCount;
        private int _totalDeletedCount;

        public bool SingleTableMode { get; }
        public bool IoStateFailed { get; private set; }

        public QuotesHistoryRepository(ILog log, IReloadingManager<string> azureStorageConnString, DataCorrectionSettings correctionSettings)
        {
            _log = log.CreateComponentScope(nameof(QuotesHistoryRepository)) ?? throw new ArgumentNullException(nameof(log));

            if (string.IsNullOrEmpty(azureStorageConnString?.CurrentValue))
                throw new ArgumentNullException(nameof(azureStorageConnString));

            if (string.IsNullOrWhiteSpace(correctionSettings.SourceTableName))
                throw new ArgumentNullException(nameof(correctionSettings.SourceTableName));
            if (string.IsNullOrWhiteSpace(correctionSettings.DestinationTableName))
                throw new ArgumentNullException(nameof(correctionSettings.DestinationTableName));

            _sourceStorage = AzureTableStorage<QuotesHistoryEntity>.Create(
                azureStorageConnString,
                correctionSettings.SourceTableName,
                _log,
                maxExecutionTimeout: TimeSpan.FromMinutes(1),
                retryDelay: TimeSpan.FromSeconds(1));

            if (correctionSettings.SourceTableName == correctionSettings.DestinationTableName)
            {
                _destStorage = _sourceStorage;
                SingleTableMode = true;
            }
            else
            {
                _destStorage = AzureTableStorage<QuotesHistoryEntity>.Create(
                    azureStorageConnString,
                    correctionSettings.DestinationTableName,
                    _log,
                    maxExecutionTimeout: TimeSpan.FromMinutes(1),
                    retryDelay: TimeSpan.FromSeconds(1)); // createTableAutomatically = true by default
            }

            _queueToInsert = new List<QuotesHistoryEntity>();
            _queueToDelete = new List<QuotesHistoryEntity>();

            _persistenceQueueMaxSize = correctionSettings.PersistenceQueueMaxSize;
            _removalQueueMaxSize = correctionSettings.RemovalQueueMaxSize;

            _startTime = DateTime.UtcNow;
        }

        public (IEnumerable<QuotesHistoryEntity> data, string token) GetDataAsync(int batchSize, string continuationToken)
        {
            return _sourceStorage.GetDataWithContinuationTokenAsync(batchSize, continuationToken).GetAwaiter().GetResult();
        }

        public bool DeleteEntity(QuotesHistoryEntity entity)
        {
            if (IoStateFailed)
                return false;

            try
            {
                _queueToDelete.Add(entity);

                if (_queueToDelete.Count >= _removalQueueMaxSize)
                    Flush(RepositoryFlushMode.FlushDelete);

                return true;
            }
            catch (Exception ex)
            {
                IoStateFailed = true;
                _log.WriteErrorAsync(nameof(DeleteEntity), null, ex).Wait();
                return false;
            }
        }

        public bool InsertEntity(QuotesHistoryEntity entity)
        {
            if (IoStateFailed)
                return false;

            try
            {
                _queueToInsert.Add(entity);

                if (_queueToInsert.Count >= _persistenceQueueMaxSize)
                    Flush(RepositoryFlushMode.FlushInsert);

                return true;
            }
            catch (Exception ex)
            {
                IoStateFailed = true;
                _log.WriteErrorAsync(nameof(InsertEntity), null, ex).Wait();
                return false;
            }
        }

        public void Flush(RepositoryFlushMode mode)
        {
            if (IoStateFailed)
                throw new InvalidOperationException("Can't flush a data to the repository in failed I/O state.");

            try
            {
                if ((mode == RepositoryFlushMode.FlushInsert || mode == RepositoryFlushMode.FlushAll) &&
                    _queueToInsert.Any())
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
                            useThrottling: true).Wait();
                    }

                    _queueToInsert = new List<QuotesHistoryEntity>();
                }

                if ((mode == RepositoryFlushMode.FlushDelete || mode == RepositoryFlushMode.FlushAll) &&
                    _queueToDelete.Any())
                {
                    foreach (var chunk in _queueToDelete.GroupBy(item => item.PartitionKey))
                    {
                        _destStorage.DeleteAsync(chunk).Wait();

                        var chunkSize = chunk.Count();
                        _totalDeletedCount += chunkSize;

                        _log.WriteInfoAsync(nameof(Flush),
                            string.Empty,
                            $"\nBatch of {chunkSize} records deleted. The first record was PK = {chunk.First().PartitionKey} & RK = {chunk.First().RowKey}, " +
                            $"the last record was PK = {chunk.Last().PartitionKey} & RK = {chunk.Last().RowKey}" +
                            $"\nThe whole process was started at {_startTime:G}, time elapsed is {DateTime.UtcNow - _startTime}. Totally deleted {_totalDeletedCount} items.",
                            useThrottling: true).Wait();
                    }

                    _queueToDelete = new List<QuotesHistoryEntity>();
                }
            }
            catch
            {
                IoStateFailed = true;
                throw;
            }
        }
    }
}
