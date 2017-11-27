using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Service.Assets.Client;
using Lykke.Service.QuotesHistory.Core.Domain.Quotes;
using Lykke.Service.QuotesHistory.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.QuotesHistory.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    public sealed class QuotesHistoryController : Controller
    {
        private readonly IQuoteHistoryRepository _quoteHistoryRepository;
        private readonly IAssetsService _assetsService;

        public QuotesHistoryController(IQuoteHistoryRepository quoteHistoryRepository, IAssetsService assetsService)
        {
            _quoteHistoryRepository = quoteHistoryRepository;
            _assetsService = assetsService;
        }

        /// <summary>
        /// Returns history values of quotes filtered by <paramref name="id"/>. If the id list is empty returns all asset pairs
        /// </summary>
        /// <param name="from">Date from inclusive</param>
        /// <param name="to">Date to exclusive</param>
        /// <param name="id">A list of quote Ids</param>
        /// <param name="cts"></param>
        /// <returns>
        /// Returns a list of quotes
        /// </returns>
        [HttpGet]
        [SwaggerOperation("QuotesHistory")]
        [ProducesResponseType(typeof(IEnumerable<QuoteResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IEnumerable<QuoteResponse>> GetQuotesHistory([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, [FromQuery]IReadOnlyCollection<string> id, CancellationTokenSource cts)
        {
            EnsureCorrectDates(from, to);
            var assetsIds = await GetAssetsIdsAsync(id);
            var quotes = await _quoteHistoryRepository.GetQuotesAsync(from.UtcDateTime, to.UtcDateTime, (IReadOnlyCollection<string>)assetsIds, cts.Token);
            var quotesModels = quotes.Select(q => new QuoteResponse(q.AssetPair, q.IsBuy, q.Price, q.Timestamp));
            GC.Collect();
            return quotesModels;
        }

        private static void EnsureCorrectDates(DateTimeOffset from, DateTimeOffset to)
        {
            var oldRowKeyFormatEnd = new DateTime(2017, 10, 1);
            if (from == default || from < oldRowKeyFormatEnd)
            {
                throw new ArgumentOutOfRangeException(nameof(from));
            }
            if (to == default)
            {
                throw new ArgumentOutOfRangeException(nameof(to));
            }

            if ((to - from).TotalDays > 7)
            {
                throw new ArgumentOutOfRangeException($"The requested period is too big {from} {to}");
            }

        }

        private async Task<IEnumerable<string>> GetAssetsIdsAsync(IReadOnlyCollection<string> id)
        {
            if (id == null || id.Count == 0)
            {
                var allAssets = await _assetsService.AssetPairGetAllAsync();
                return allAssets.Select(a => a.Id).ToArray();
            }
            return id;
        }
    }
}
