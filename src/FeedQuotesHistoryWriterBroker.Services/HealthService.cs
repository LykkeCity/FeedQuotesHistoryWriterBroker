using System.Collections.Generic;
using FeedQuotesHistoryWriterBroker.Core.Domain.Health;
using FeedQuotesHistoryWriterBroker.Core.Services;

namespace FeedQuotesHistoryWriterBroker.Services
{
    // NOTE: See https://lykkex.atlassian.net/wiki/spaces/LKEWALLET/pages/35755585/Add+your+app+to+Monitoring
    public class HealthService : IHealthService
    {
        public string GetHealthViolationMessage()
        {
            // TODO: Check gathered health statistics, and return appropriate health violation message, or NULL if job hasn't critical errors
            return null;
        }

        public IEnumerable<HealthIssue> GetHealthIssues()
        {
            var issues = new HealthIssuesCollection();

            // TODO: Check gathered health statistics, and add appropriate health issues message to issues
            return issues;
        }
    }
}