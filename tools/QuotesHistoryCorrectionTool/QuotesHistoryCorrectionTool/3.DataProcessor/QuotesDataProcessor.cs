using System;
using System.Linq;
using Common.Log;
using QuotesHistoryCorrectionTool.Repository;

// ReSharper disable once CheckNamespace
namespace QuotesHistoryCorrectionTool.DataProcessor
{
    public class QuotesDataProcessor
    {
        private readonly ILog _log;
        private readonly IQuotesHistoryRepository _repo;
        private string _continuationToken;
        private readonly int _queryBatchSize;

        public QuotesDataProcessor(ILog log, IQuotesHistoryRepository repo, int queryBatchSize)
        {
            _log = log?.CreateComponentScope(nameof(QuotesDataProcessor)) ??
                   throw new ArgumentNullException(nameof(log));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _continuationToken = string.Empty;
            _queryBatchSize = queryBatchSize;
        }

        public bool Execute()
        {
            try
            {
                do
                {
                    var (data, token) = _repo.GetDataAsync(_queryBatchSize, _continuationToken);
                    _continuationToken = token;

                    // ReSharper disable once PossibleMultipleEnumeration
                    if (!data.Any())
                        break;
                    
                    // ReSharper disable once PossibleMultipleEnumeration
                    foreach (var entity in data)
                    {
                        if (entity.RkType == QuotesHistoryEntity.RowKeyType.Literals)
                        {
                            if (_repo.SingleTableMode)
                                continue;
                            
                            if (!_repo.InsertEntity(entity))
                                throw new InvalidOperationException(
                                    $"Couldn't insert the entity with PK = {entity.PartitionKey} and RK = {entity.RowKey}, please, see logs for more details. Can't proceed.");

                            continue;
                        }

                        var newEntity = entity.SwitchRowKeyType();

                        if (!_repo.InsertEntity(newEntity))
                            throw new InvalidOperationException(
                                $"Couldn't insert the entity with PK = {newEntity.PartitionKey} and RK = {newEntity.RowKey}, please, see logs for more details. Can't proceed.");

                        if (!_repo.SingleTableMode)
                            continue;

                        if (!_repo.DeleteEntity(entity))
                            throw new InvalidOperationException(
                                $"Couldn't delete the entity with PK = {entity.PartitionKey} and RK = {entity.RowKey}, please, see logs for more details. Can't proceed.");
                    }
                } while (!string.IsNullOrWhiteSpace(_continuationToken));

                _repo.Flush();

                return true;
            }
            catch (Exception ex)
            {
                _log.WriteFatalErrorAsync(nameof(Execute), string.Empty, ex).Wait();
                return false;
            }
        }
    }
}

