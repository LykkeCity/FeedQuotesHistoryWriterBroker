using System;
using Lykke.Domain.Prices.Contracts;
using Newtonsoft.Json;

namespace Lykke.Service.QuotesHistory.Models
{
    public sealed class QuoteResponse : IQuote
    {
        public QuoteResponse(string assetPair, bool isBuy, double price, DateTime timestamp)
        {
            AssetPair = assetPair;
            IsBuy = isBuy;
            Price = price;
            Timestamp = timestamp;
        }

        [JsonProperty("assetPair")]
        public string AssetPair { get; set; }

        [JsonProperty("isBuy")]
        public bool IsBuy { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
