// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using ImageGallery.Logging;
using ImageGallery.Store.AzureStorage;
using ImageGallery.Store.Cosmos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageGallery.Tests
{
    internal class ServiceFactory : WebApplicationFactory<Startup>
    {
        internal readonly IBlobContainerProvider AzureStorageProvider;
        internal readonly IClientProvider CosmosClientProvider;
        private IDatabaseProvider CosmosDbProvider;

        private readonly MockLogger Logger;

        internal ServiceFactory(Mocks.Cosmos.MockCosmosState cosmosState, MockLogger logger)
        {
            this.AzureStorageProvider = new Mocks.AzureStorage.MockBlobContainerProvider(logger);
            this.CosmosClientProvider = new Mocks.Cosmos.MockClientProvider(cosmosState, logger);
            this.Logger = logger;
        }

        internal async Task<IDatabaseProvider> InitializeCosmosDbAsync()
        {
            this.CosmosDbProvider = await this.CosmosClientProvider.CreateDatabaseIfNotExistsAsync(Constants.DatabaseName);
            await this.CosmosDbProvider.CreateContainerIfNotExistsAsync(Constants.AccountCollectionName, "/id");
            return this.CosmosDbProvider;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices((context, services) =>
            {
                // Inject the mock logger that writes to the console.
                services.AddSingleton<ILogger<ApplicationLogs>>(this.Logger);

                // Inject the mocks.
                services.AddSingleton(this.AzureStorageProvider);
                services.AddSingleton(this.CosmosDbProvider);
            });
        }

        protected override TestServer CreateServer(IWebHostBuilder builder)
        {
            return base.CreateServer(builder);
        }

        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return base.CreateWebHostBuilder();
        }
    }
}
