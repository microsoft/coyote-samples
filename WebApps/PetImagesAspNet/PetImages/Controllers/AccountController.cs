// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Entities;
using PetImages.Exceptions;
using PetImages.Storage;

namespace PetImages.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly ICosmosContainer AccountContainer;

        public AccountController(ICosmosContainer accountContainer)
        {
            this.AccountContainer = accountContainer;
        }

        /// <summary>
        /// Scenario 1: Buggy CreateAccountAsync version.
        /// </summary>
        [HttpPost("create")]
        [Produces(typeof(ActionResult<Account>))]
        public async Task<ActionResult<Account>> CreateAccountAsync(Account account)
        {
            Console.WriteLine($"[1] CreateAccountAsync: thread '{System.Threading.Thread.CurrentThread.ManagedThreadId}' - '{System.Threading.SynchronizationContext.Current}': {new System.Diagnostics.StackTrace()}");
            var accountItem = account.ToItem();

            Console.WriteLine($"[2] CreateAccountAsync");
            if (await StorageHelper.DoesItemExist<AccountItem>(
                this.AccountContainer,
                accountItem.PartitionKey,
                accountItem.Id))
            {
                return this.Conflict();
            }

            Console.WriteLine($"[3] CreateAccountAsync");
            var createdAccountItem = await this.AccountContainer.CreateItem(accountItem);

            Console.WriteLine($"[4] CreateAccountAsync");
            return this.Ok(createdAccountItem.ToAccount());
        }

        /// <summary>
        /// Scenario 1: Fixed CreateAccountAsync version.
        /// </summary>
        [HttpPost("create-fixed")]
        public async Task<ActionResult<Account>> CreateAccountAsyncFixed(Account account)
        {
            var accountItem = account.ToItem();

            try
            {
                accountItem = await this.AccountContainer.CreateItem(accountItem);
            }
            catch (DatabaseItemAlreadyExistsException)
            {
                return this.Conflict();
            }

            return this.Ok(accountItem.ToAccount());
        }
    }
}
