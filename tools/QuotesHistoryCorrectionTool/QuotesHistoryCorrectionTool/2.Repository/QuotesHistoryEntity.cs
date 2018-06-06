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
        public RowKeyType RkType => DetectRkType().type;

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
            (var rkType, var date) = DetectRkType();
            

            switch (rkType)
            {
                case RowKeyType.Ticks:
                    return new QuotesHistoryEntity(PartitionKey, date.ToString("yyyy-MM-ddTHH:mm:00"), Part000);

                case RowKeyType.Literals:
                    return new QuotesHistoryEntity(PartitionKey, string.Format("{0:D19}", date.Ticks), Part000);

                // Neverhood
                default:
                    throw new InvalidOperationException($"Something wrong: the entity with PK = {PartitionKey} RK = {RowKey} has unrecognized row key type.");
            }
        }
        
        #region Private

        private (RowKeyType type, DateTime value) DetectRkType()
        {
            if (string.IsNullOrEmpty(RowKey))
                throw new ArgumentNullException(nameof(RowKey));
            
            // Ticks is like 0636274006200000000
            // Literals is like 2017-03-13T09:27:00

            // Just a primitive check for we actually do have only 2 possible RK types in the table.

            switch (RowKey.Length)
            {
                case 19 when RowKey.Contains("-") && RowKey.Contains("T") && RowKey.Contains(":"):
                    if (!DateTime.TryParse(RowKey, out var date))
                        throw new ArgumentException($"Couldn't detect the entity type with PK = {PartitionKey} and RK = {RowKey}: The input string '{RowKey}' can not be converted to DateTime object.");
                    return (type: RowKeyType.Literals, value: date);

                case 19:
                    date = new DateTime(long.Parse(RowKey), DateTimeKind.Utc);
                    return (type: RowKeyType.Ticks, value: date);

                default:
                    throw new ArgumentException($"Couldn't detect the entity type with PK = {PartitionKey} and RK = {RowKey}: The input string '{RowKey}' is not recognized as a proper date-time string of the format Ticks or Literals.");
            }
        }

        #endregion
    }
}
