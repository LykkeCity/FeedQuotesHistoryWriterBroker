using System;
using System.Collections.Generic;
using System.Linq;
using Common.Log;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuotesHistoryCorrectionTool.Repository;
using Moq;
using QuotesHistoryCorrectionTool.DataProcessor;

namespace UnitTests
{
    [TestClass]
    public class QuotesDataProcessorTest
    {
        public static List<QuotesHistoryEntity> Data;
        private static List<QuotesHistoryEntity> _dataCopy;
        public static List<string> CorrectDates;

        #region Init

        [TestInitialize]
        public void InitializeTest()
        {
            Data = new List<QuotesHistoryEntity>
            {
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:15:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:16:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:26:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:27:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:25:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:16:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:26:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:27:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:15:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:25:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:16:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:25:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:15:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:27:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:26:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:15:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:16:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:25:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:26:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:27:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274005000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274005600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274006200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274006800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274007400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274008000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274008600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274009200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274009800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274010400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274011000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274011600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274012200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274012800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274013400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274014000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274014600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274015200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274015800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274016400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274017000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274017600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274018200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274018800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274019400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274020000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274020600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274021200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274021800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274022400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274023000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274023600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274024200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274024800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274025400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274026000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274026600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274027200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274027800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274028400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274029000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274029600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274030200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274030800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274031400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274032000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274032600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274033200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274033800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274034400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274035000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274035600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274036200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274036800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274037400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274038000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274038600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274039200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274039800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274040400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274041000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274041600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274042200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274042800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274043400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274044000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274044600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274045200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274045800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274046400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274047000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274047600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274048200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274048800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274049400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274050000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274050600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274051200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274051800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274052400000000", "Sample data")
            };

            // This is the copy of Data which is needed to avoid the "sequence has changed while iteration's been performed" case only.
            // The QuotesDataProcessor will read from Data and write to _dataCopy using the corresponding Mock (see below).
            _dataCopy = new List<QuotesHistoryEntity>();

            CorrectDates = new List<string>
            {
                "2017-03-13T09:15:00",
                "2017-03-13T09:16:00",
                "2017-03-13T09:26:00",
                "2017-03-13T09:27:00",
                "2017-03-13T09:25:00",
                "2017-03-13T09:16:00",
                "2017-03-13T09:26:00",
                "2017-03-13T09:27:00",
                "2017-03-13T09:15:00",
                "2017-03-13T09:25:00",
                "2017-03-13T09:16:00",
                "2017-03-13T09:25:00",
                "2017-03-13T09:15:00",
                "2017-03-13T09:27:00",
                "2017-03-13T09:26:00",
                "2017-03-13T09:15:00",
                "2017-03-13T09:16:00",
                "2017-03-13T09:25:00",
                "2017-03-13T09:26:00",
                "2017-03-13T09:27:00",
                "2017-04-10T05:55:00",
                "2017-04-10T05:56:00",
                "2017-04-10T05:57:00",
                "2017-04-10T05:58:00",
                "2017-04-10T05:59:00",
                "2017-04-10T06:00:00",
                "2017-04-10T06:01:00",
                "2017-04-10T06:02:00",
                "2017-04-10T06:03:00",
                "2017-04-10T06:04:00",
                "2017-04-10T06:05:00",
                "2017-04-10T06:06:00",
                "2017-04-10T06:07:00",
                "2017-04-10T06:08:00",
                "2017-04-10T06:09:00",
                "2017-04-10T06:10:00",
                "2017-04-10T06:11:00",
                "2017-04-10T06:12:00",
                "2017-04-10T06:13:00",
                "2017-04-10T06:14:00",
                "2017-04-10T06:15:00",
                "2017-04-10T06:16:00",
                "2017-04-10T06:17:00",
                "2017-04-10T06:18:00",
                "2017-04-10T06:19:00",
                "2017-04-10T06:20:00",
                "2017-04-10T06:21:00",
                "2017-04-10T06:22:00",
                "2017-04-10T06:23:00",
                "2017-04-10T06:24:00",
                "2017-04-10T06:25:00",
                "2017-04-10T06:26:00",
                "2017-04-10T06:27:00",
                "2017-04-10T06:28:00",
                "2017-04-10T06:29:00",
                "2017-04-10T06:30:00",
                "2017-04-10T06:31:00",
                "2017-04-10T06:32:00",
                "2017-04-10T06:33:00",
                "2017-04-10T06:34:00",
                "2017-04-10T06:35:00",
                "2017-04-10T06:36:00",
                "2017-04-10T06:37:00",
                "2017-04-10T06:38:00",
                "2017-04-10T06:39:00",
                "2017-04-10T06:40:00",
                "2017-04-10T06:41:00",
                "2017-04-10T06:42:00",
                "2017-04-10T06:43:00",
                "2017-04-10T06:44:00",
                "2017-04-10T06:45:00",
                "2017-04-10T06:46:00",
                "2017-04-10T06:47:00",
                "2017-04-10T06:48:00",
                "2017-04-10T06:49:00",
                "2017-04-10T06:50:00",
                "2017-04-10T06:51:00",
                "2017-04-10T06:52:00",
                "2017-04-10T06:53:00",
                "2017-04-10T06:54:00",
                "2017-04-10T06:55:00",
                "2017-04-10T06:56:00",
                "2017-04-10T06:57:00",
                "2017-04-10T06:58:00",
                "2017-04-10T06:59:00",
                "2017-04-10T07:00:00",
                "2017-04-10T07:01:00",
                "2017-04-10T07:02:00",
                "2017-04-10T07:03:00",
                "2017-04-10T07:04:00",
                "2017-04-10T07:05:00",
                "2017-04-10T07:06:00",
                "2017-04-10T07:07:00",
                "2017-04-10T07:08:00",
                "2017-04-10T07:09:00",
                "2017-04-10T07:10:00",
                "2017-04-10T07:11:00",
                "2017-04-10T07:12:00",
                "2017-04-10T07:13:00",
                "2017-04-10T07:14:00"
            };
        }

        #endregion

        [TestMethod]
        public void Process_data_test()
        {
            // Arrange
            var logMock = new Mock<ILog>();
            var repoMock = new QuotesHistoryRepositoryMock();
            var processor = new QuotesDataProcessor(logMock.Object, repoMock, 1_000);

            // Act
            var result = processor.Execute();

            var resultingDates =
                (from d in _dataCopy
                 select d.RowKey).ToList();

            var notMatched1 = CorrectDates.Except(resultingDates);
            var notMatched2 = resultingDates.Except(CorrectDates);

            // Assert
            Assert.IsTrue(result);

            Assert.AreEqual(_dataCopy.Count, CorrectDates.Count);

            Assert.IsFalse(notMatched1.Any());
            Assert.IsFalse(notMatched2.Any());
        }

        #region Mocks

        public class QuotesHistoryRepositoryMock : IQuotesHistoryRepository
        {
            public bool SingleTableMode => true;

            public bool IoStateFailed => false;

            public (IEnumerable<QuotesHistoryEntity> data, string token) GetDataAsync(int batchSize, string continuationToken)
            {
                var sizeLimit = Math.Min(batchSize, Data.Count);

                return (data: Data.Take(sizeLimit), token: string.Empty); // We have the single batch for tests.
            }
            
            public bool InsertEntity(QuotesHistoryEntity entity)
            {
                _dataCopy.Add(entity);
                return true;
            }

            public void Flush()
            {
            }
        }

        #endregion
    }
}
