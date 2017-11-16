using System;
using Lykke.Domain.Prices.Contracts;
using Newtonsoft.Json;

namespace Lykke.Service.QuotesHistory.Repositories
{
    public class QuoteItem : IQuote
    {
        [JsonProperty("A")]
        public string AssetPair { get; set; }
        [JsonProperty("B")]
        public bool IsBuy { get; set; }
        [JsonProperty("P")]
        public double Price { get; set; }
        [JsonProperty("T")]
        public DateTime Timestamp { get; set; }
    }
}
