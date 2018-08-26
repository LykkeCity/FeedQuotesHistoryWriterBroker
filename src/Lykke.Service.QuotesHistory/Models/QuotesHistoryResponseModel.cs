using System.Collections.Generic;

namespace Lykke.Service.QuotesHistory.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class QuotesHistoryResponseModel
    {
        public IEnumerable<HumanReadableQuote> Quotes { get; set; }
        public string ContinuationToken { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
