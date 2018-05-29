using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace QuotesHistoryCorrectionTool.Repository
{
    public enum RepositoryFlushMode
    {
        FlushInsert,
        FlushDelete,
        FlushAll
    }

    // The following interface is mainly needed for testing purpose.
    public interface IQuotesHistoryRepository
    {
        bool SingleTableMode { get; }
        bool IoStateFailed { get; }

        (IEnumerable<QuotesHistoryEntity> data, string token) GetDataAsync(int batchSize, string continuationToken);
        bool DeleteEntity(QuotesHistoryEntity entity);
        bool InsertEntity(QuotesHistoryEntity entity);
        void Flush(RepositoryFlushMode mode = RepositoryFlushMode.FlushAll);
    }
}
