using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.QuotesHistory.Core.Domain.Quotes;
using Lykke.Service.QuotesHistory.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Lykke.Service.QuotesHistory.Controllers
{
    [Route("api/[controller]")]
    public class HistoryController : Controller
    {
        private readonly IQuoteHistoryRepository _historyRepository;

        public HistoryController(IQuoteHistoryRepository historyRepository)
        {
            _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
        }

        /// <summary>
        /// Obtains quotes history by request parameters. NOTE: this method is only allowed to be used internally in bounds of the AlgoStore project.
        /// </summary>
        /// <param name="assetPair">Asset pair ID.</param>
        /// <param name="priceType">Price type: Bid or Ask.</param>
        /// <param name="fromMoment">The earliest quote timestamp (exclusive).</param>
        /// <param name="toMoment">The latest quote timestamp (inclusive).</param>
        /// <param name="continuationToken">Continuation token returned by the previous query (if any).</param>
        [HttpGet]
        [SwaggerOperation("History")]
        [ProducesResponseType(typeof(QuotesHistoryResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Get(string assetPair, PriceType priceType, DateTime fromMoment, DateTime toMoment, string continuationToken = null)
        {
            if (string.IsNullOrEmpty(assetPair))
                return BadRequest("Asset pair should not have empty value.");
            // If we've got some incorrect date-time value, the corresponding param will be assigned with the default value (actually,  DatTime.MinValue).
            if (fromMoment == default)
                return BadRequest("Starting time stamp is malformed.");
            if (toMoment == default)
                return BadRequest("Ending time stamp is malformed.");
            if (fromMoment >= toMoment)
                return BadRequest("Starting time stamp for the query should be earlier than the ending time stamp.");

            try
            {
                var quotes = await _historyRepository.GetQuotesBulkAsync(assetPair, priceType == PriceType.Ask, fromMoment, toMoment, continuationToken);

                return Ok(new QuotesHistoryResponseModel
                {
                    Quotes = quotes.Quotes.Select(QuotesHistoryResponseItem.FromIQuote),
                    ContinuationToken = quotes.ContinuationToken
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ErrorResponse.Create(ex.Message));
            }
        }
    }
}
