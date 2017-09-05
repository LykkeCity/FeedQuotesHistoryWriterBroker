using Autofac;
using Common;

namespace FeedQuotesHistoryWriterBroker.Core.Services.Quotes
{
    public interface IQuotesBroker : IStartable, IStopable
    {
    }
}