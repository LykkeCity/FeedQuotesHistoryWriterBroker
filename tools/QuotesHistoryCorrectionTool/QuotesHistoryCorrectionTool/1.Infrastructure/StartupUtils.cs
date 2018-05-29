using AzureStorage.Tables;
using Common.Log;
using Lykke.Logs;
using Lykke.Logs.Slack;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace QuotesHistoryCorrectionTool.Infrastructure
{
    public static class StartupUtils
    {
        public static IConfigurationRoot LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
        }

        public static ILog CreateLogWithSlack(SlackNotificationSettings slackSettings, IReloadingManager<string> dbLogConnectionStringManager)
        {
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();

            aggregateLogger.AddLog(consoleLogger);

            var services = new ServiceCollection();

            var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new Lykke.AzureQueueIntegration.AzureQueueSettings
            {
                ConnectionString = slackSettings.AzureQueue.ConnectionString,
                QueueName = slackSettings.AzureQueue.QueueName
            }, aggregateLogger);

            var dbLogConnectionString = dbLogConnectionStringManager.CurrentValue;

            if (!(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                    AzureTableStorage<LogEntity>.Create(dbLogConnectionStringManager, "QuotesHistoryCorrectionToolLogs", consoleLogger),
                    consoleLogger);

                var slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(slackService, consoleLogger);

                var azureStorageLogger = new LykkeLogToAzureStorage(
                    persistenceManager,
                    slackNotificationsManager,
                    consoleLogger);

                azureStorageLogger.Start();

                aggregateLogger.AddLog(azureStorageLogger);
            }

            var logToSlack = LykkeLogToSlack.Create(slackService, "Prices");

            aggregateLogger.AddLog(logToSlack);

            return aggregateLogger;
        }
    }
}
