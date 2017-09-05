using System.Threading.Tasks;
using Lykke.Domain.Prices.Model;

namespace FeedQuotesHistoryWriterBroker.Core.Services.Quotes
{
    public interface IQuotesManager
    {
        Task ConsumeQuote(Quote quote);
        Task PersistQuotes();
    }
}