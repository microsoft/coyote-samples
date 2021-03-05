// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Coyote.Samples.AccountManager.ETags
{
    public static class Program
    {
        public static async Task Main()
        {
            await TestConcurrentAccountCreation();
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task TestAccountCreation()
        {
            // Initialize the mock in-memory DB and account manager.
            var dbCollection = new InMemoryDbCollection();
            var accountManager = new AccountManager(dbCollection);

            // Create some dummy data.
            string accountName = "MyAccount";
            string accountPayload = "...";

            // Create the account, it should complete successfully and return true.
            var result = await accountManager.CreateAccount(accountName, accountPayload);
            Assert.True(result);

            // Create the same account again. The method should return false this time.
            result = await accountManager.CreateAccount(accountName, accountPayload);
            Assert.False(result);
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task TestConcurrentAccountCreation()
        {
            // Initialize the mock in-memory DB and account manager.
            var dbCollection = new InMemoryDbCollection();
            var accountManager = new AccountManager(dbCollection);

            // Create some dummy data.
            string accountName = "MyAccount";
            string accountPayload = "...";

            // Call CreateAccount twice without awaiting, which makes both methods run
            // asynchronously with each other.
            var task1 = accountManager.CreateAccount(accountName, accountPayload);
            // await Task.Delay(1); // Enable artificial delay to make bug harder to manifest.
            var task2 = accountManager.CreateAccount(accountName, accountPayload);

            // Then wait both requests to complete.
            await Task.WhenAll(task1, task2);

            // Finally, assert that only one of the two requests succeeded and the other
            // failed. Note that we do not know which one of the two succeeded as the
            // requests ran concurrently (this is why we use an exclusive OR).
            Assert.True(task1.Result ^ task2.Result);
        }
    }
}
