using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using MoreLinq;
using Newtonsoft.Json;

namespace Lykke.Service.QuotesHistory.Repositories
{
    public class QuoteTableEntity : ITableEntity
    {
        public QuoteTableEntity()
        {
        }

        public QuoteTableEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        #region ITableEntity properties

        public string ETag { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        #endregion

        public List<QuoteItem> Quotes { get; private set; } = new List<QuoteItem>();

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            // Fields are expected to be: Part000, Part001, Part002, ... , Part251
            // Join fields in order and deserialize quotes.
            //
            var dataFields = properties.Keys.Where(key => key.StartsWith("Part")).ToList();
            dataFields.Sort();

            var content = new StringBuilder();
            foreach (var dataField in dataFields)
            {
                // Check that field's name is "Part<number>"
                if (properties.TryGetValue(dataField, out var property) && Int32.TryParse(dataField.Substring(4), out var cell))
                {
                    content.Append(property.StringValue);
                }
            }

            this.Quotes = JsonConvert.DeserializeObject<List<QuoteItem>>(content.ToString());
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            // Serialize all quotes
            // Split to 64Kb parts
            // Write each part to separate field

            var content = JsonConvert.SerializeObject(this.Quotes);
            var parts = content.Batch(32000).Select(collection => new string(collection.ToArray())).ToArray();

            var dict = new Dictionary<string, EntityProperty>();
            for(var i = 0; i < parts.Length; i++)
            {
                dict.Add("Part" + i.ToString("D3"), new EntityProperty(parts[i]));
            }
            return dict;
        }

        public static string GeneratePartitionKey(string assetPairId, bool isBuy)
        {
            return $"{assetPairId}_{(isBuy ? "BUY" : "SELL")}";
        }

        public static string GenerateRowKey(DateTime timestamp)
        {
            return timestamp.ToString("yyyy-MM-ddTHH:mm:00");
        }
    }
}
