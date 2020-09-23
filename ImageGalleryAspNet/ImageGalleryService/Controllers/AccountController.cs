// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using ImageGallery.Models;
using ImageGallery.Store.AzureStorage;
using ImageGallery.Store.Cosmos;
using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.Controllers
{
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IContainerProvider AccountContainer;
        private readonly IBlobContainerProvider StorageProvider;
        private readonly TextWriter Logger;

        public AccountController(IDatabaseProvider databaseProvider, IBlobContainerProvider storageProvider, TextWriter logger)
        {
            this.AccountContainer = databaseProvider.GetContainer(Constants.AccountCollectionName);
            this.StorageProvider = storageProvider;
            this.Logger = logger;
        }

        [HttpPut]
        [Produces(typeof(ActionResult<Account>))]
        [Route("api/account/create")]
        public async Task<ActionResult<Account>> Create(Account account)
        {
            this.Logger.WriteLine("Creating account with id '{0}' (name: '{1}', email: '{2}').",
                account.Id, account.Name, account.Email);

            // Check if the account exists in Cosmos DB.
            var exists = await this.AccountContainer.ExistsItemAsync<AccountEntity>(account.Id, account.Id);
            if (exists)
            {
                return this.NotFound();
            }

            // BUG: calling create on the Cosmos DB container after checking if the account exists is racy
            // and can, for example, fail due to another concurrent request. Typically someone could write
            // a create or update request, that uses the `UpsertItemAsync` Cosmos DB API, but we dont use
            // it here just for the purposes of this buggy sample service.

            // The account does not exist, so create it in Cosmos DB.
            var entity = await this.AccountContainer.CreateItemAsync(new AccountEntity(account));
            return this.Ok(entity.GetAccount());
        }

        [HttpPut]
        [Produces(typeof(ActionResult<Account>))]
        [Route("api/account/update")]
        public async Task<ActionResult<Account>> Update(Account account)
        {
            this.Logger.WriteLine("Updating account with id '{0}' (name: '{1}', email: '{2}').",
                account.Id, account.Name, account.Email);

            // Check if the account exists in Cosmos DB.
            var exists = await this.AccountContainer.ExistsItemAsync<AccountEntity>(account.Id, account.Id);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling update on the Cosmos DB container after checking if the account exists is racy
            // and can, for example, fail due to another concurrent request. This throws an exception
            // that the controller does not handle, and thus is reported as a 500. This can be fixed
            // by properly handling ReplaceItemAsync and returning a `NotFound` instead.

            // Update the account in Cosmos DB.
            var entity = await this.AccountContainer.ReplaceItemAsync(new AccountEntity(account));
            return this.Ok(entity.GetAccount());
        }

        [HttpGet]
        [Produces(typeof(ActionResult<Account>))]
        [Route("api/account/get/")]
        public async Task<ActionResult<Account>> Get(string id)
        {
            this.Logger.WriteLine("Getting account with id '{0}'.", id);

            // Check if the account exists in Cosmos DB.
            var exists = await this.AccountContainer.ExistsItemAsync<AccountEntity>(id, id);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling get on the Cosmos DB container after checking if the account exists is racy
            // and can, for example, fail due to another concurrent request that deleted the account.

            // The account exists, so get it from Cosmos DB.
            var entity = await this.AccountContainer.ReadItemAsync<AccountEntity>(id, id);
            return this.Ok(entity.GetAccount());
        }

        [HttpDelete]
        [Produces(typeof(ActionResult))]
        [Route("api/account/delete/")]
        public async Task<ActionResult> Delete(string id)
        {
            this.Logger.WriteLine("Deleting account with id '{0}'.", id);

            // Check if the account exists in Cosmos DB.
            var exists = await this.AccountContainer.ExistsItemAsync<AccountEntity>(id, id);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling the following APIs after checking if the account exists is racy and can
            // fail due to another concurrent request.

            // The account exists, so delete it from Cosmos DB.
            await this.AccountContainer.DeleteItemAsync<AccountEntity>(id, id);

            // Finally, if there is an image container for this account, then also delete it.
            var containerName = Constants.GetContainerName(id);
            await this.StorageProvider.DeleteContainerIfExistsAsync(containerName);

            return this.Ok();
        }
    }
}
