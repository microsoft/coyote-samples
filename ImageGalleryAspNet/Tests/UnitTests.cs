// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ImageGallery.Logging;
using ImageGallery.Models;
using ImageGallery.Store.Cosmos;
using ImageGallery.Tests.Mocks.Cosmos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImageGallery.Tests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public async Task TestConcurrentAccountRequestsAsync()
        {
            var logger = new MockLogger();
            var cosmosState = new MockCosmosState(logger);

            using var factory = new ServiceFactory(cosmosState, logger);
            await factory.InitializeCosmosDbAsync();

            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions()
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

            // Try create a new account, and wait for it to be created before proceeding with the test.
            var account = new Account("0", "alice", "alice@coyote.com");
            var createAccountRes = await client.PutAsJsonAsync(new Uri($"api/account/create", UriKind.RelativeOrAbsolute), account);
            createAccountRes.EnsureSuccessStatusCode();

            var updatedAccount = new Account("0", "alice", "alice@microsoft.com");

            // Try update the account and delete it concurrently, which can cause a data race and a bug.
            var updateTask = client.PutAsJsonAsync(new Uri($"api/account/update", UriKind.RelativeOrAbsolute), updatedAccount);
            var deleteTask = client.DeleteAsync(new Uri($"api/account/delete?id={updatedAccount.Id}", UriKind.RelativeOrAbsolute));

            // Wait for the two concurrent requests to complete.
            await Task.WhenAll(updateTask, deleteTask);

            // Bug: the update request can nondeterministically fail due to an unhandled exception (500 error code).
            // See the `Update` handler in the account controller for more info.
            var updateAccountRes = updateTask.Result;
            if (!(updateAccountRes.StatusCode == HttpStatusCode.OK || updateAccountRes.StatusCode == HttpStatusCode.NotFound))
            {
                throw new AssertFailedException("Found unexpected error code.");
            }

            var deleteAccountRes = deleteTask.Result;
            deleteAccountRes.EnsureSuccessStatusCode();
        }

        [TestMethod]
        public async Task TestConcurrentAccountAndImageRequestsAsync()
        {
            var logger = new MockLogger();
            var cosmosState = new MockCosmosState(logger);

            using var factory = new ServiceFactory(cosmosState, logger);
            IDatabaseProvider databaseProvider = await factory.InitializeCosmosDbAsync();

            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions()
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

            // Try create a new account, and wait for it to be created before proceeding with the test.
            var account = new Account("0", "alice", "alice@coyote.com");
            var createAccountRes = await client.PutAsJsonAsync(new Uri($"api/account/create", UriKind.RelativeOrAbsolute), account);
            createAccountRes.EnsureSuccessStatusCode();

            // Try store the image and delete the account concurrently, which can cause a data race and a bug.
            var image = new Image(account.Id, "beach", Encoding.Default.GetBytes("waves"));
            var storeImageTask = client.PutAsJsonAsync(new Uri($"api/gallery/store", UriKind.RelativeOrAbsolute), image);
            var deleteAccountTask = client.DeleteAsync(new Uri($"api/account/delete?id={account.Id}", UriKind.RelativeOrAbsolute));

            // Wait for the two concurrent requests to complete.
            await Task.WhenAll(storeImageTask, deleteAccountTask);

            // BUG: The above two concurrent requests can race and result into the image being stored
            // in an "orphan" container in Azure Storage, even if the associated account was deleted.

            // Check that the image was deleted from Azure Storage.
            var exists = await factory.AzureStorageProvider.ExistsBlobAsync(Constants.GetContainerName(account.Id), image.Name);
            if (exists)
            {
                throw new AssertFailedException("The image was not deleted from Azure Blob Storage.");
            }

            // Check that the account was deleted from Cosmos DB.
            var accountContainer = databaseProvider.GetContainer(Constants.AccountCollectionName);
            exists = await accountContainer.ExistsItemAsync<AccountEntity>(account.Id, account.Id);
            if (exists)
            {
                throw new AssertFailedException("The account was not deleted from Cosmos DB.");
            }
        }
    }
}
