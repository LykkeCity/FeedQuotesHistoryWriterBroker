using System.Threading.Tasks;
using Lykke.Domain.Prices.Model;

namespace Lykke.Service.QuotesHistory.Core.Services.Quotes
{
    public interface IQuotesManager
    {
        Task ConsumeQuote(Quote quote);
        Task PersistQuotes();
    }
}