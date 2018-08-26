using Lykke.Domain.Prices.Contracts;
using System;

namespace Lykke.Service.QuotesHistory.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class HumanReadableQuote
    {
        public PriceType PriceType { get; set; }
        public double Price { get; set; }
        public DateTime Timestamp { get; set; }

        public static HumanReadableQuote FromIQuote(IQuote q)
        {
            if (q == null) { throw new ArgumentNullException(nameof(q)); }

            return new HumanReadableQuote
            {
                PriceType = q.IsBuy ? PriceType.Ask : PriceType.Bid,
                Price = q.Price,
                Timestamp = q.Timestamp
            };
        }
    }

    public enum PriceType
    {
        Ask,
        Bid
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
