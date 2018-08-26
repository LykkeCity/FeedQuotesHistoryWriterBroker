using System.Threading.Tasks;
using Lykke.Domain.Prices.Model;

namespace Lykke.Service.QuotesHistory.Core.Services.Quotes
{
    public interface IQuotesManager
    {
        void ConsumeQuote(Quote quote);
        Task PersistQuotes();
    }
}
