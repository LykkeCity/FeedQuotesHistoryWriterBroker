using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.Assets.Client;
using Lykke.Service.QuotesHistory.Core.Domain.Quotes;
using Lykke.Service.QuotesHistory.Core.Services;
using Lykke.Service.QuotesHistory.Core.Services.Quotes;
using Lykke.Service.QuotesHistory.Core.Settings;
using Lykke.Service.QuotesHistory.Repositories;
using Lykke.Service.QuotesHistory.Services;
using Lykke.Service.QuotesHistory.Services.Quotes;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;


namespace Lykke.Service.QuotesHistory.Modules
{
    public class JobModule : Module
    {
        private readonly IReloadingManager<AppSettings.QuotesHistorySettings> _serviceSettings;
        private readonly IReloadingManager<AppSettings.AssetsSettings> _assetsSettings;
        private readonly ILog _log;
        private readonly ServiceCollection _services;

        public JobModule(IReloadingManager<AppSettings.QuotesHistorySettings> serviceSettings, IReloadingManager<AppSettings.AssetsSettings> assetsSettings, ILog log)
        {
            _serviceSettings = serviceSettings;
            _assetsSettings = assetsSettings;
            _log = log;
            _services = new ServiceCollection();

        }

        private void RegisterAssetsServices()
        {
            var assets = _assetsSettings.CurrentValue;
            _services.RegisterAssetsClient(AssetServiceSettings.Create(new Uri(assets.ServiceUrl), assets.CacheExpirationPeriod));

        }

        protected override void Load(ContainerBuilder builder)
        {
            RegisterAssetsServices();
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();


            builder.Register(c => new QuoteHistoryRepository(
                AzureTableStorage<QuoteTableEntity>.Create(
                    _serviceSettings.ConnectionString(x => x.ConnectionStrings.HistoryConnectionString), "QuotesHistory", _log,
                    onModificationRetryCount: _serviceSettings.CurrentValue.StorageRetrySettings.OnModificationsRetryCount,
                    onGettingRetryCount: _serviceSettings.CurrentValue.StorageRetrySettings.OnGettingRetryCount,
                    retryDelay: _serviceSettings.CurrentValue.StorageRetrySettings.RetryDelay,
                    maxExecutionTimeout: _serviceSettings.CurrentValue.StorageRetrySettings.ExecutionTimeout)
            )).As<IQuoteHistoryRepository>();

            builder.RegisterType<QuotesManager>()
                .As<IQuotesManager>();

            builder.RegisterType<QuotesBroker>()
                .As<IStartable>()
                .As<IQuotesBroker>()
                .WithParameter(TypedParameter.From(_serviceSettings.CurrentValue.RabbitMq.ConnectionString))
                .AutoActivate()
                .SingleInstance();

            builder.Populate(_services);
        }
    }
}
