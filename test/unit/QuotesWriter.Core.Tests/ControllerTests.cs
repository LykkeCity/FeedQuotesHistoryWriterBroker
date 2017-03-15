using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Common.Log;
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices.Model;

namespace QuotesWriter.Core.Tests
{
    public class ControllerTests
    {
        [Fact]
        public void ControllerGroupsQuotesByAsset()
        {
            var log = new LogToMemory();

            var repo = new Mock<IQuoteHistoryRepository>();
            repo
                .Setup(r => r.InsertOrMergeAsync(It.IsAny<IEnumerable<IQuote>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(0));

            var baseTime = new DateTime(2017, 03, 12, 7, 8, 9, DateTimeKind.Utc);

            var controller = new Controller(repo.Object, log);

            controller.ConsumeQuote(new Quote() { AssetPair = "EURUSD", IsBuy = true, Price = 10, Timestamp = baseTime }).Wait();
            controller.ConsumeQuote(new Quote() { AssetPair = "EURUSD", IsBuy = true, Price = 20, Timestamp = baseTime.AddSeconds(1) }).Wait();
            controller.ConsumeQuote(new Quote() { AssetPair = "BTCUSD", IsBuy = true, Price = 30, Timestamp = baseTime }).Wait();
            controller.ConsumeQuote(new Quote() { AssetPair = "BTCUSD", IsBuy = true, Price = 31, Timestamp = baseTime.AddSeconds(1) }).Wait();

            controller.ConsumeQuote(new Quote() { AssetPair = "EURUSD", IsBuy = false, Price = 40, Timestamp = baseTime }).Wait();
            controller.ConsumeQuote(new Quote() { AssetPair = "EURUSD", IsBuy = false, Price = 50, Timestamp = baseTime.AddSeconds(1) }).Wait();
            controller.ConsumeQuote(new Quote() { AssetPair = "BTCUSD", IsBuy = false, Price = 60, Timestamp = baseTime }).Wait();
            controller.ConsumeQuote(new Quote() { AssetPair = "BTCUSD", IsBuy = false, Price = 70, Timestamp = baseTime.AddSeconds(1) }).Wait();

            // Controller must group quotes by Asset and IsBuy sign
            controller.Tick().Wait();

            // Verify calls to repository. Repository must be called for each group separately
            //
            repo.Verify(r => r.InsertOrMergeAsync(It.IsAny<IEnumerable<IQuote>>(), "EURUSD", true), Times.Once());
            repo.Verify(r => r.InsertOrMergeAsync(It.IsAny<IEnumerable<IQuote>>(), "EURUSD", false), Times.Once());
            repo.Verify(r => r.InsertOrMergeAsync(It.IsAny<IEnumerable<IQuote>>(), "BTCUSD", true), Times.Once());
            repo.Verify(r => r.InsertOrMergeAsync(It.IsAny<IEnumerable<IQuote>>(), "BTCUSD", false), Times.Once());
        }
    }
}
