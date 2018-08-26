using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices.Model;
using Lykke.Logs;
using Lykke.Service.QuotesHistory.Core.Domain.Quotes;
using Lykke.Service.QuotesHistory.Services.Quotes;
using Moq;
using Xunit;

namespace Lykke.Service.QuotesHistory.Tests
{
    public class ControllerTests
    {
        [Fact]
        public void ControllerGroupsQuotesByAsset()
        {
            var repo = new Mock<IQuoteHistoryRepository>();
            repo
                .Setup(r => r.InsertOrMergeAsync(It.IsAny<IReadOnlyCollection<IQuote>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(0));

            var baseTime = new DateTime(2017, 03, 12, 7, 8, 9, DateTimeKind.Utc);

            var manager = new QuotesManager(repo.Object, EmptyLogFactory.Instance);

            manager.ConsumeQuote(new Quote() { AssetPair = "EURUSD", IsBuy = true, Price = 10, Timestamp = baseTime });
            manager.ConsumeQuote(new Quote() { AssetPair = "EURUSD", IsBuy = true, Price = 20, Timestamp = baseTime.AddSeconds(1) });
            manager.ConsumeQuote(new Quote() { AssetPair = "BTCUSD", IsBuy = true, Price = 30, Timestamp = baseTime });
            manager.ConsumeQuote(new Quote() { AssetPair = "BTCUSD", IsBuy = true, Price = 31, Timestamp = baseTime.AddSeconds(1) });

            manager.ConsumeQuote(new Quote() { AssetPair = "EURUSD", IsBuy = false, Price = 40, Timestamp = baseTime });
            manager.ConsumeQuote(new Quote() { AssetPair = "EURUSD", IsBuy = false, Price = 50, Timestamp = baseTime.AddSeconds(1) });
            manager.ConsumeQuote(new Quote() { AssetPair = "BTCUSD", IsBuy = false, Price = 60, Timestamp = baseTime });
            manager.ConsumeQuote(new Quote() { AssetPair = "BTCUSD", IsBuy = false, Price = 70, Timestamp = baseTime.AddSeconds(1) });

            // Controller must group quotes by Asset and IsBuy sign
            manager.PersistQuotes().Wait();

            // Verify calls to repository. Repository must be called for each group separately
            //
            repo.Verify(r => r.InsertOrMergeAsync(It.IsAny<IReadOnlyCollection<IQuote>>(), "EURUSD", true), Times.Once());
            repo.Verify(r => r.InsertOrMergeAsync(It.IsAny<IReadOnlyCollection<IQuote>>(), "EURUSD", false), Times.Once());
            repo.Verify(r => r.InsertOrMergeAsync(It.IsAny<IReadOnlyCollection<IQuote>>(), "BTCUSD", true), Times.Once());
            repo.Verify(r => r.InsertOrMergeAsync(It.IsAny<IReadOnlyCollection<IQuote>>(), "BTCUSD", false), Times.Once());
        }
    }
}
