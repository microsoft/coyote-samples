namespace PetImages.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using PetImages.Contracts;
    using PetImages.Entities;
    using PetImages.Exceptions;
    using PetImages.Storage;
    using System.Threading.Tasks;

    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private ICosmosContainer cosmosContainer;

        public AccountController(ICosmosContainer cosmosDb)
        {
            this.cosmosContainer = cosmosDb;
        }

        // Scenario 1: Buggy PutAsync version
        [HttpPost]
        public async Task<ActionResult<Account>> PutAsync(Account account)
        {
            var accountItem = account.ToItem();

            if (await StorageHelper.DoesItemExist<AccountItem>(
                cosmosContainer,
                accountItem.PartitionKey,
                accountItem.Id))
            {
                return this.Conflict();
            }

            var createdAccountItem = await cosmosContainer.CreateItem(accountItem);

            return this.Ok(createdAccountItem.ToAccount());
        }

        // Scenario 1: Fixed PutAsync version
        public async Task<ActionResult<Account>> PutAsyncFixed(Account account)
        {
            var accountItem = account.ToItem();

            try
            {
                accountItem = await cosmosContainer.CreateItem(accountItem);
            }
            catch (DatabaseItemAlreadyExistsException)
            {
                return this.Conflict();
            }

            return this.Ok(accountItem.ToAccount());
        }   
    }
}
