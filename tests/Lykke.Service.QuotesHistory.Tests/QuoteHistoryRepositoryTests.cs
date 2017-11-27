using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Domain.Prices.Model;
using Lykke.Service.Assets.Client;
using Lykke.Service.QuotesHistory.Repositories;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;
using Moq;
using Xunit;

namespace Lykke.Service.QuotesHistory.Tests
{
    public class QuoteHistoryRepositoryTests
    {
        private readonly LogToMemory _log;
        private readonly QuoteHistoryRepository _repo;

        public QuoteHistoryRepositoryTests()
        {
            _log = new LogToMemory();
            var storage = CreateStorage<QuoteTableEntity>();
            _repo = new QuoteHistoryRepository(storage);

        }


        [Fact(Skip = "Uses a real connection string")]
        public async Task GetSize()
        {
            var rm = new Mock<IReloadingManager<string>>();
            var conn = "";
            rm.Setup(m => m.CurrentValue).Returns(conn);
            rm.Setup(m => m.Reload()).Returns(Task.FromResult(conn));

            var stor = AzureTableStorage<QuoteTableEntity>.Create(rm.Object, "QuotesHistory", _log, TimeSpan.FromHours(1));
            var rep = new QuoteHistoryRepository(stor);
            var assetSrvice = new AssetsService(new Uri("http://assets.lykke-service.svc.cluster.local"));
            var allAssetPairs = await assetSrvice.AssetPairGetAllAsync();
            var pairs = allAssetPairs.Select(ap => ap.Id).ToArray();
            var quotes = await rep.GetQuotesAsync(DateTime.UtcNow.AddDays(-9), DateTime.UtcNow.AddDays(-2), pairs, CancellationToken.None);
            Assert.True(quotes.Any());

        }

        /// <summary>
        /// Two quotes with the same asset and isBuy sign can have different timestamp, and they will be both stored.
        /// </summary>
        [Fact(Skip = "Requires Azure table emulator")]
        public void RepositoryCanStoreQuotesWithSameTimestamp()
        {



            var asset = "EURUSD";
            var baseTime = new DateTime(2017, 03, 12, 13, 14, 05, DateTimeKind.Utc);
            var quotes = new List<Quote>
            {
                new Quote { AssetPair=asset, IsBuy = true, Price = 1, Timestamp = baseTime }
            };

            // Insert the same quote two times
            _repo.InsertOrMergeAsync(quotes, asset, isBuy: true).Wait();
            _repo.InsertOrMergeAsync(quotes, asset, isBuy: true).Wait();

            // Storage must contain two instances.
            var storedQuotes = _repo.GetQuotesAsync(asset, true, baseTime).Result;

            Assert.NotNull(storedQuotes);
            Assert.Equal(2, storedQuotes.Count());
            Assert.Equal(0, _log.Count);
        }

        /// <summary>
        /// Repository can store quotes with different row keys in one call
        /// </summary>
        [Fact(Skip = "Requires Azure table emulator")]
        public void RepositoryCanStoreMultipleRows()
        {

            var asset = "EURUSD";
            var baseTime = new DateTime(2017, 03, 12, 13, 14, 05, DateTimeKind.Utc);
            var quotes = new List<Quote>
            {
                new Quote { AssetPair=asset, IsBuy = true, Price = 1, Timestamp = baseTime },
                new Quote { AssetPair=asset, IsBuy = true, Price = 2, Timestamp = baseTime.AddMinutes(1).AddSeconds(1) },
                new Quote { AssetPair=asset, IsBuy = false, Price = 3, Timestamp = baseTime },
                new Quote { AssetPair=asset, IsBuy = false, Price = 4, Timestamp = baseTime.AddMinutes(1).AddSeconds(2) }
            };

            // This call will insert only Buy prices
            _repo.InsertOrMergeAsync(quotes, asset, isBuy: true).Wait();
            // This call will insert only Sell prices
            _repo.InsertOrMergeAsync(quotes, asset, isBuy: false).Wait();

            // Storage contains 4 rows
            var storedQuotes = _repo.GetQuotesAsync(asset, true, baseTime).Result;
            Assert.Single(storedQuotes);
            storedQuotes = _repo.GetQuotesAsync(asset, true, baseTime.AddMinutes(1)).Result;
            Assert.Single(storedQuotes);

            storedQuotes = _repo.GetQuotesAsync(asset, false, baseTime).Result;
            Assert.Single(storedQuotes);
            storedQuotes = _repo.GetQuotesAsync(asset, false, baseTime.AddMinutes(1)).Result;
            Assert.Single(storedQuotes);

            Assert.Equal(0, _log.Count);
        }

        /// <summary>
        /// Large quote collections will be distributed across separate properties ("columns").
        /// </summary>
        [Fact(Skip = "Requires Azure table emulator")]
        public void RepositoryCanUtilizeMultipleProperties()
        {
            var asset = "EURUSD";
            var baseTime = new DateTime(2017, 03, 12, 13, 14, 05, DateTimeKind.Utc);
            var quotes = new List<Quote>();
            // Create a lot of quotes in one second (will be placed in one row)
            for (int i = 0; i < 2000; i++)
            {
                quotes.Add(new Quote { AssetPair = asset, IsBuy = true, Price = i / 10, Timestamp = baseTime.AddMilliseconds(i / 2) });
            }

            // Insert all quotes
            _repo.InsertOrMergeAsync(quotes, asset, isBuy: true).Wait();

            // Storage must be able to read quotes.
            var storedQuotes = _repo.GetQuotesAsync(asset, true, baseTime).Result;

            Assert.NotNull(storedQuotes);
            Assert.Equal(2000, storedQuotes.Count());
            Assert.Equal(0, _log.Count);
        }

        [Fact(Skip = "Requires Azure table emulator")]
        public async Task ShouldGetQuotesForPeriodByAsset()
        {
            var quotes = new List<Quote>();
            var startDate = new DateTime(2017, 03, 1, 13, 14, 05, DateTimeKind.Utc);
            var assets = new[] { "EURUSD", "BTCUSD" };
            foreach (var asset in assets)
            {
                for (int d = 0; d < 10; d++)
                {
                    var baseTime = startDate.AddDays(d);
                    // Create a lot of quotes in one second (will be placed in one row)
                    for (int i = 0; i < 2000; i++)
                    {
                        quotes.Add(new Quote { AssetPair = asset, IsBuy = true, Price = i / 10, Timestamp = baseTime.AddMilliseconds(i / 2) });
                    }
                    for (int i = 0; i < 2000; i++)
                    {
                        quotes.Add(new Quote { AssetPair = asset, IsBuy = false, Price = i / 11, Timestamp = baseTime.AddMilliseconds(i / 3) });
                    }

                    // Insert all quotes
                }
                _repo.InsertOrMergeAsync(quotes, asset, isBuy: true).Wait();
                _repo.InsertOrMergeAsync(quotes, asset, isBuy: false).Wait();

                // Storage must be able to read quotes.
                var storedQuotes = await _repo.GetQuotesAsync(startDate, startDate.AddDays(1), new[] { asset }, CancellationToken.None);

                Assert.NotNull(storedQuotes);
                Assert.Equal(4000, storedQuotes.Count());
                Assert.Equal(0, _log.Count);
            }
        }

        private INoSQLTableStorage<T> CreateStorage<T>() where T : class, ITableEntity, new()
        {
            var rm = new Mock<IReloadingManager<string>>();
            rm.Setup(m => m.CurrentValue).Returns(@"UseDevelopmentStorage=true");
            rm.Setup(m => m.Reload()).Returns(Task.FromResult(@"UseDevelopmentStorage=true"));
            var stor = AzureTableStorage<T>.Create(rm.Object, "QuotesHistory" + DateTime.UtcNow.Ticks, _log, TimeSpan.FromHours(1));
            return stor;
        }
    }
}
