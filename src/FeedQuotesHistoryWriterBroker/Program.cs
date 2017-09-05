using System;
using AzureStorage.Tables;
using Common.Application;
using Common.Log;
using FeedQuotesHistoryWriterBroker.Settings;
using Flurl.Http;
using Lykke.Logs;
using Lykke.SlackNotification.AzureQueue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FeedQuotesHistoryWriterBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var log = new AggregateLogger();
            var consoleLog = new LogToConsole();

            log.AddLog(consoleLog);
;
            try
            {
                log.Info("Reading application settings.");

                var config = new ConfigurationBuilder()
                    //.AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                var settingsUrl = config.GetValue<string>("SettingsUrl");

                log.Info("Loading app settings from web-site.");
                var appSettings = LoadSettings(settingsUrl);

                log.Info("Initializing azure/slack logger.");

                var services = new ServiceCollection(); // only used for azure logger

                UseLogWithSlack(services, appSettings, log, consoleLog);
                
                // After log is configured
                //
                log.Info("Creating Startup.");
                var startup = new Startup(appSettings);

                log.Info("Configure startup services.");
                startup.ConfigureServices(Application.Instance.ContainerBuilder, log);

                log.Info("Starting application.");
                var scope = Application.Instance.Start();

                log.Info("Configure startup.");
                startup.Configure(scope);

                log.Info("Running application.");
                Application.Instance.Run();

                log.Info("Exit application.");
            }
            catch (Exception ex)
            {
                log.WriteErrorAsync("Program", string.Empty, string.Empty, ex).Wait();
            }
        }

        private static AppSettings LoadSettings(string url)
        {
            return url.GetJsonAsync<AppSettings>().Result;
        }

        private static void UseLogWithSlack(IServiceCollection services, AppSettings settings, AggregateLogger aggregateLog, ILog consoleLog)
        {
            // Creating slack notification service, which logs own azure queue processing messages to aggregate log
            var slackService = services.UseSlackNotificationsSenderViaAzureQueue(settings.SlackNotifications.AzureQueue, aggregateLog);
            var dbLogConnectionString = settings.FeedQuotesHistoryWriterBroker.ConnectionStrings.LogsConnectionString;

            // Creating azure storage logger, which logs own messages to concole log
            if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                var appName = Startup.ApplicationName;

                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                    appName,
                    AzureTableStorage<LogEntity>.Create(() => dbLogConnectionString, $"{appName}Logs", consoleLog),
                    consoleLog);

                var slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(appName, slackService, consoleLog);

                var azureStorageLogger = new LykkeLogToAzureStorage(
                    appName,
                    persistenceManager,
                    slackNotificationsManager,
                    consoleLog);

                azureStorageLogger.Start();

                aggregateLog.AddLog(azureStorageLogger);
            }
        }
    }

    internal static class LogExtensions
    {
        public static void Info(this ILog log, string info)
        {
            log.WriteInfoAsync("FeedQuotesHistoryWriterBroker", "Program", string.Empty, info).Wait();
        }
    }
}
