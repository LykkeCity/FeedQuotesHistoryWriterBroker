using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuotesHistoryCorrectionTool.Repository;

namespace UnitTests
{
    [TestClass]
    public class QuotesHistoryEntityTest
    {
        private List<QuotesHistoryEntity> _entityListD19;
        private List<QuotesHistoryEntity> _entityListO;

        #region Init

        [TestInitialize]
        public void InitializeTest()
        {
            _entityListD19 = new List<QuotesHistoryEntity>{
                new QuotesHistoryEntity("AHAU100_BUY", "0636274005000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274005600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274006200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274006800000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "0636274007400000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "0636274008000000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "0636274008600000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "0636274009200000000", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "0636274009800000000", "Sample data"),
            };

            _entityListO = new List<QuotesHistoryEntity>{
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:15:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:16:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:26:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:27:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_BUY", "2017-03-13T09:25:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:16:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:26:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:27:00", "Sample data"),
                new QuotesHistoryEntity("AHAU100_SELL", "2017-03-13T09:15:00", "Sample data"),
            };
        }

        #endregion

        [TestMethod]
        public void Row_type_detection_type_O()
        {
            // Arrange
            
            // Act
            var entD19 = _entityListO.Where(e => e.RkType != QuotesHistoryEntity.RowKeyType.Literals);

            // Assert
            Assert.IsFalse(entD19.Any());
        }

        [TestMethod]
        public void Row_type_detection_type_D19()
        {
            // Arrange
            
            // Act
            var entO = _entityListD19.Where(e => e.RkType != QuotesHistoryEntity.RowKeyType.Ticks);

            // Assert
            Assert.IsFalse(entO.Any());
        }

        [TestMethod]
        public void Switch_row_key_type_D19_to_O()
        {
            // Arrange
            var correctKeys = new List<string>
            {
                "2017-04-10T05:55:00",
                "2017-04-10T05:56:00",
                "2017-04-10T05:57:00",
                "2017-04-10T05:58:00",
                "2017-04-10T05:59:00",
                "2017-04-10T06:00:00",
                "2017-04-10T06:01:00",
                "2017-04-10T06:02:00",
                "2017-04-10T06:03:00"
            };

            // Act
            var switchedList = 
                (from e in _entityListD19
                select e.SwitchRowKeyType().RowKey).ToList();

            var notMatched1 = correctKeys.Except(correctKeys);
            var notMatched2 = switchedList.Except(correctKeys);

            // Assert
            Assert.IsFalse(notMatched1.Any());
            Assert.IsFalse(notMatched2.Any());
        }

        [TestMethod]
        public void Switch_row_key_type_O_D19()
        {
            // Arrange
            var correctKeys = new List<string>
            {
                "0636249933000000000",
                "0636249933600000000",
                "0636249939600000000",
                "0636249940200000000",
                "0636249939000000000",
                "0636249933600000000",
                "0636249939600000000",
                "0636249940200000000",
                "0636249933000000000"
            };

            // Act
            var switchedList =
                (from e in _entityListO
                    select e.SwitchRowKeyType().RowKey).ToList();

            var notMatched1 = correctKeys.Except(correctKeys);
            var notMatched2 = switchedList.Except(correctKeys);

            // Assert
            Assert.IsFalse(notMatched1.Any());
            Assert.IsFalse(notMatched2.Any());
        }
    }
}
