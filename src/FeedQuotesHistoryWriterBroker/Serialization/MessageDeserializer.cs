using System.Text;
using Lykke.Domain.Prices.Model;
using Lykke.RabbitMqBroker.Subscriber;
using Newtonsoft.Json;

namespace FeedQuotesHistoryWriterBroker.Serialization
{
    public class MessageDeserializer : IMessageDeserializer<Quote>
    {
        public Quote Deserialize(byte[] data)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc // treat datetime as Utc
            };

            string json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<Quote>(json, settings);
        }
    }
}
