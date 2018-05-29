using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

// ReSharper disable once CheckNamespace
namespace QuotesHistoryCorrectionTool.Repository
{
    public class QuotesHistoryEntity : ITableEntity
    {
        public enum RowKeyType
        {
            Ticks,
            Literals
        }

        public string ETag { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public string Part000 { get; set; }
        public RowKeyType RkType => DetectRkType(RowKey).type;

        // For using with AzureTableStorage
        public QuotesHistoryEntity()
        {
        }

        public QuotesHistoryEntity(string pk, string rk, string part000)
        {
            PartitionKey = pk;
            RowKey = rk;
            Part000 = part000;
        }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            if (properties.TryGetValue("Part000", out var property))
                Part000 = property.StringValue;
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var dict = new Dictionary<string, EntityProperty>
            {
                {"Part000", new EntityProperty(Part000)}
            };

            return dict;
        }

        public QuotesHistoryEntity SwitchRowKeyType()
        {
            (var rkType, var date) = DetectRkType(RowKey);

            switch (rkType)
            {
                case RowKeyType.Ticks:
                    return new QuotesHistoryEntity(PartitionKey, date.ToString("yyyy-MM-ddTHH:mm:00"), Part000);

                case RowKeyType.Literals:
                    return new QuotesHistoryEntity(PartitionKey, string.Format("{0:D19}", date.Ticks), Part000);

                // Neverhood
                default:
                    throw new InvalidOperationException("Something wrong: the entity has unrecognized row key type.");
            }
        }
        
        #region Private

        private static (RowKeyType type, DateTime value) DetectRkType(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));
            
            // Ticks is like 0636274006200000000
            // Literals is like 2017-03-13T09:27:00

            // Just a primitive check for we actually do have only 2 possible RK types in the table.

            switch (value.Length)
            {
                case 19 when value.Contains("-") && value.Contains("T") && value.Contains(":"):
                    if (!DateTime.TryParse(value, out var date))
                        throw new ArgumentException($"The input string '{value}' can not be converted to DateTime object.");
                    return (type: RowKeyType.Literals, value: date);

                case 19:
                    date = new DateTime(long.Parse(value), DateTimeKind.Utc);
                    return (type: RowKeyType.Ticks, value: date);

                default:
                    throw new ArgumentException($"The input string '{value}' is not recognized as a proper date-time string of the format Ticks or Literals.");
            }
        }

        #endregion
    }
}
