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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class HistoryController : Controller
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private readonly IQuoteHistoryRepository _historyRepository;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public HistoryController(IQuoteHistoryRepository historyRepository)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
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
            if (fromMoment > toMoment)
                return BadRequest("Starting time stamp for the query should not be later than the ending time stamp.");

            // ---

            try
            {
#pragma warning disable IDE0042 // Deconstruct variable declaration
                var quotes = await _historyRepository.GetQuotesBulkAsync(assetPair, priceType == PriceType.Ask, fromMoment, toMoment, continuationToken);
#pragma warning restore IDE0042 // Deconstruct variable declaration

                return Ok(new QuotesHistoryResponseModel
                {
                    Quotes = quotes.Quotes.Select(q => HumanReadableQuote.FromIQuote(q)),
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
