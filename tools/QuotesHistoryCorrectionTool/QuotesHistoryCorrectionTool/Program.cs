using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.SettingsReader;
using QuotesHistoryCorrectionTool.Infrastructure;
using QuotesHistoryCorrectionTool.Repository;
using QuotesHistoryCorrectionTool.DataProcessor;

namespace QuotesHistoryCorrectionTool
{
    internal static class Program
    {
        static string EnvInfo => Environment.GetEnvironmentVariable("ENV_INFO");
        const int BatchSize = 1_000; // Currently, this is Azure Storage maximum batch size for a single query.

        // ReSharper disable once UnusedParameter.Local
        public static void Main(string[] args)
        {
            Console.WriteLine($"QuotesHistoryCorrectionTool version {Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion}");
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif
            Console.WriteLine($"ENV_INFO: {EnvInfo}");

            ILog log = null;

            try
            {
                Console.WriteLine($"{DateTime.UtcNow} - Starting the process of quotes history RK correction...");

                var config = StartupUtils.LoadConfiguration();
                var settings = config.LoadSettings<AppSettings>();
                log = StartupUtils.CreateLogWithSlack(settings.CurrentValue.SlackNotification,
                    settings.ConnectionString(x => x.ConnectionStrings.LogsConnectionString));
                log.SetInfoMessagesTimeout(settings.CurrentValue.DataCorrection.InfoMessagesTimeout);

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    log.WriteFatalErrorAsync(nameof(Program), nameof(Main), (Exception)e?.ExceptionObject ?? new Exception("Unknown error.")).Wait();
                    Console.WriteLine($"Unhandled exception: {e?.ExceptionObject}");

                    if (e?.IsTerminating == true)
                    {
                        Console.WriteLine("Terminating...");
                    }
                };

                var repo = new QuotesHistoryRepository(log,
                    settings.ConnectionString(x => x.ConnectionStrings.SourceHistoryConnectionString),
                    settings.ConnectionString(x => x.ConnectionStrings.DestinationHistoryConnectionString),
                    settings.CurrentValue.DataCorrection);

                // ---

                var processor = new QuotesDataProcessor(log, repo, BatchSize);

                Console.WriteLine(processor.Execute()
                    ? $"\n{DateTime.UtcNow} - That's done.\n"
                    : $"\n{DateTime.UtcNow} - The process was interrupted by an error. See logs for clarification.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n{DateTime.UtcNow} - Fatal error:");
                Console.WriteLine(ex);

                log?.WriteFatalErrorAsync(nameof(Program), nameof(Main), ex).Wait();
            }

            // Lets devops to see end of process in console before exit.
            var delay = TimeSpan.FromMinutes(1);

            Console.WriteLine();
            Console.WriteLine($"Process will be terminated in {delay}. Press any key to terminate immediately.");

            Task.WhenAny(
                    Task.Delay(delay),
                    Task.Run(() => { Console.ReadKey(true); }))
                .Wait();
        }
    }
}
