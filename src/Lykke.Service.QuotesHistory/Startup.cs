using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Service.QuotesHistory.Core.Services.Quotes;
using Lykke.Service.QuotesHistory.Core.Settings;
using Lykke.Service.QuotesHistory.Modules;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.QuotesHistory
{
    internal sealed class Startup
    {
        private IContainer ApplicationContainer { get; set; }
        private IConfigurationRoot Configuration { get; }
        private ILog Log { get; set; }
        private IHealthNotifier HealthNotifier { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddMvc()
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.Converters.Add(new StringEnumConverter());
                        options.SerializerSettings.ContractResolver =
                            new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    });

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration("v1", "Lykke.Service.QuotesHistory API");

                    //Determine base path for the application.
                    var basePath = PlatformServices.Default.Application.ApplicationBasePath;

                    //Set the comments path for the swagger json and ui.
                    var xmlPath = Path.Combine(basePath, "Lykke.Service.QuotesHistory.xml");
                    options.IncludeXmlComments(xmlPath);
                });

                var builder = new ContainerBuilder();
                var appSettings = Configuration.LoadSettings<AppSettings>(options =>
                {
                    options.SetConnString(x => x.SlackNotifications.AzureQueue.ConnectionString);
                    options.SetQueueName(x => x.SlackNotifications.AzureQueue.QueueName);
                    options.SenderName = "Lykke.Service.QuotesHistory";
                });

                services.AddLykkeLogging(
                    appSettings.ConnectionString(x => x.QuotesHistoryService.ConnectionStrings.LogsConnectionString),
                    "FeedQuotesHistoryWriterBrokerLogs",
                    appSettings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                    appSettings.CurrentValue.SlackNotifications.AzureQueue.QueueName
                    );

                builder.Populate(services);

                builder.RegisterModule(new JobModule(appSettings.Nested(x => x.QuotesHistoryService)));
                
                ApplicationContainer = builder.Build();

                Log = ApplicationContainer.Resolve<ILogFactory>().CreateLog(this);

                HealthNotifier = ApplicationContainer.Resolve<IHealthNotifier>();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseLykkeMiddleware(ex => new ErrorResponse { ErrorMessage = "Technical problem" });

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(x =>
            {
                x.RoutePrefix = "swagger/ui";
                x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            });
            app.UseStaticFiles();

            appLifetime.ApplicationStarted.Register(StartApplication);
            appLifetime.ApplicationStopping.Register(StopApplication);
            appLifetime.ApplicationStopped.Register(CleanUp);
        }

        private void StartApplication()
        {
            try
            {
                HealthNotifier?.Notify("Started");
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }

        private void StopApplication()
        {
            try
            {
                var broker = ApplicationContainer.Resolve<IQuotesBroker>();

                broker.Stop();
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
            }
        }

        private void CleanUp()
        {
            try
            {
                HealthNotifier?.Notify("Terminating");

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    Log.Critical(ex);
                    (Log as IDisposable)?.Dispose();
                }
            }
        }
    }
}
