// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Coyote.Samples.AccountManager
{
    public static class Program
    {
        private static bool RunningMain = false;

        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
            }

            RunningMain = true;

            foreach (var arg in args)
            {
                if (arg[0] == '-')
                {
                    switch (arg.ToUpperInvariant().Trim('-'))
                    {
                        case "S":
                            Console.WriteLine("Running sequential test without Coyote ...");
                            await TestAccountCreation();
                            Console.WriteLine("Done.");
                            return;
                        case "C":
                            Console.WriteLine("Running concurrent test without Coyote ...");
                            await TestConcurrentAccountCreation();
                            Console.WriteLine("Done.");
                            return;
                        case "?":
                        case "H":
                        case "HELP":
                            PrintUsage();
                            return;
                        default:
                            Console.WriteLine("### Unknown arg: " + arg);
                            PrintUsage();
                            return;
                    }
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: AccountManager [option]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -s    Run sequential test without Coyote");
            Console.WriteLine("  -c    Run concurrent test without Coyote");
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task TestAccountCreation()
        {
            CheckRewritten();

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
            CheckRewritten();

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

        private static void CheckRewritten()
        {
            if (!RunningMain && !Rewriting.RewritingEngine.IsAssemblyRewritten(typeof(Program).Assembly))
            {
                throw new Exception(string.Format("Error: please rewrite this assembly using coyote rewrite {0}",
                    typeof(Program).Assembly.Location));
            }
        }
    }
}
