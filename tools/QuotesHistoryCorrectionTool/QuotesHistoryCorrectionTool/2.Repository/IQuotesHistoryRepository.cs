using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace QuotesHistoryCorrectionTool.Repository
{
    // The following interface is mainly needed for testing purpose.
    public interface IQuotesHistoryRepository
    {
        bool IoStateFailed { get; }

        (IEnumerable<QuotesHistoryEntity> data, string token) GetDataAsync(int batchSize, string continuationToken);
        bool InsertEntity(QuotesHistoryEntity entity);
        void Flush();
    }
}
