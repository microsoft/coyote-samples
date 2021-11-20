﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PetImages;
using PetImages.Contracts;
using PetImages.Tests.MessagingMocks;
using PetImages.Tests.StorageMocks;

#pragma warning disable SA1005
namespace PetImages.Tests
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public async Task TestFirstScenario()
        {
            // Initialize the in-memory service factory.
            using var factory = new ServiceFactory();
            await factory.InitializeAccountContainerAsync();
            await factory.InitializeImageContainerAsync();

            using var client = new ServiceClient(factory);

            // Create an account request payload.
            var account = new Account()
            {
                Name = "MyAccount"
            };

            // Call 'CreateAccount' twice without awaiting, which makes both methods run
            // asynchronously with each other.
            var task1 = client.CreateAccountAsync(account);
            var task2 = client.CreateAccountAsync(account);

            // Then wait both requests to complete.
            await Task.WhenAll(task1, task2);

            // Finally, assert that only one of the two requests succeeded and the other
            // failed. Note that we do not know which one of the two succeeded as the
            // requests ran concurrently (this is why we use an exclusive OR).
            Assert.IsTrue(
               (task1.Result == HttpStatusCode.OK && task2.Result == HttpStatusCode.Conflict) ||
               (task1.Result == HttpStatusCode.Conflict && task2.Result == HttpStatusCode.OK));
        }

        [TestMethod]
        public async Task TestSecondScenario()
        {
           // Initialize the in-memory service factory.
           using var factory = new ServiceFactory();
           await factory.InitializeAccountContainerAsync();
           var imageContainer = await factory.InitializeImageContainerAsync();

           using var client = new ServiceClient(factory);

           string accountName = "MyAccount";
           string imageName = "pet.jpg";

           // Create an account request payload
           var account = new Account()
           {
               Name = accountName
           };

           var accountResult = await client.CreateAccountAsync(account);
           Assert.IsTrue(accountResult == HttpStatusCode.OK);

           imageContainer.EnableRandomizedFaults();

           var task1 = client.CreateImageAsync(accountName,
               new Image() { Name = imageName, Content = GetDogImageBytes() });
           var task2 = client.CreateImageAsync(accountName,
               new Image() { Name = imageName, Content = GetDogImageBytes() });
           await Task.WhenAll(task1, task2);

           imageContainer.DisableRandomizedFaults();

           Assert.IsTrue(task1.Result == HttpStatusCode.OK || task1.Result == HttpStatusCode.Conflict ||
               task1.Result == HttpStatusCode.ServiceUnavailable);
           Assert.IsTrue(task2.Result == HttpStatusCode.OK || task2.Result == HttpStatusCode.Conflict ||
               task2.Result == HttpStatusCode.ServiceUnavailable);

           if (task1.Result == HttpStatusCode.OK || task2.Result == HttpStatusCode.OK)
           {
               var (statusCode, content) = await client.GetImageAsync(accountName, imageName);
               Assert.IsTrue(statusCode == HttpStatusCode.OK);
               Assert.IsTrue(IsDogImage(content));
           }
        }

        //[TestMethod]
        //public async Task TestThirdScenario()
        //{
        //    // Initialize the in-memory service factory.
        //    // var cosmosState = new MockCosmosState();
        //    // var database = new MockCosmosDatabase(cosmosState);
        //    // var accountContainer = (MockCosmosContainer)await database.CreateContainerAsync(Constants.AccountContainerName);
        //    // var imageContainer = (MockCosmosContainer)await database.CreateContainerAsync(Constants.ImageContainerName);
        //    // var blobContainer = new MockBlobContainerProvider();
        //    // var messagingClient = new MockMessagingClient(blobContainer);
        //    // var petImagesClient = new ServiceClient(accountContainer, imageContainer, blobContainer, messagingClient);
        //    using var factory = new ServiceFactory();
        //    await factory.InitializeAccountContainerAsync();
        //    await factory.InitializeImageContainerAsync();

        //    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions()
        //    {
        //        AllowAutoRedirect = false,
        //        HandleCookies = false
        //    });

        //    string accountName = "MyAccount";
        //    string imageName = "pet.jpg";

        //    // Create an account request payload
        //    var account = new Account()
        //    {
        //        Name = accountName
        //    };

        //    var accountResult = await petImagesClient.CreateAccountAsync(account);
        //    Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

        //    var task1 = petImagesClient.CreateOrUpdateImageAsync(accountName, new Image() { Name = imageName, Content = GetDogImageBytes() });
        //    var task2 = petImagesClient.CreateOrUpdateImageAsync(accountName, new Image() { Name = imageName, Content = GetCatImageBytes() });
        //    await Task.WhenAll(task1, task2);

        //    Assert.IsTrue(task1.Result.StatusCode == HttpStatusCode.OK);
        //    Assert.IsTrue(task1.Result.StatusCode == HttpStatusCode.OK);

        //    var imageResult = await petImagesClient.GetImageAsync(accountName, imageName);
        //    Assert.IsTrue(imageResult.StatusCode == HttpStatusCode.OK);
        //    byte[] image = imageResult.Resource;

        //    byte[] thumbnail;
        //    while (true)
        //    {
        //        var thumbnailResult = await petImagesClient.GetImageThumbnailAsync(accountName, imageName);
        //        if (thumbnailResult.StatusCode == HttpStatusCode.OK)
        //        {
        //            thumbnail = thumbnailResult.Resource;
        //            break;
        //        }
        //    }

        //    Assert.IsTrue(
        //        (IsDogImage(image) && IsDogThumbnail(thumbnail)) ||
        //        (IsCatImage(image) && IsCatThumbnail(thumbnail)));
        //}

        [TestMethod]
        public void SystematicTestFirstScenario()
        {
            RunSystematicTest(this.TestFirstScenario);
        }

        //[TestMethod]
        //public void SystematicTestSecondScenario()
        //{
        //    RunSystematicTest(this.TestSecondScenario);
        //}

        //[TestMethod]
        //public void SystematicTestThirdScenario()
        //{
        //    RunSystematicTest(this.TestThirdScenario);
        //}

        /// <summary>
        /// Invoke the Coyote systematic testing engine to run the specified test multiple iterations,
        /// each iteration exploring potentially different interleavings using some underlying program
        /// exploration strategy (by default a uniform probabilistic strategy).
        /// </summary>
        /// <remarks>
        /// Learn more in our documentation: https://microsoft.github.io/coyote/how-to/unit-testing
        /// </remarks>
        private static void RunSystematicTest(Func<Task> test, string reproducibleScheduleFilePath = null)
        {
            // Configuration for how to run a concurrency unit test with Coyote.
            // This configuration will run the test 1000 times exploring different paths each time.
            var config = Configuration.Create().WithTestingIterations(1000);

            if (reproducibleScheduleFilePath != null)
            {
                var trace = File.ReadAllText(reproducibleScheduleFilePath);
                config = config.WithReplayStrategy(trace);
            }

            var testingEngine = TestingEngine.Create(config, test);

            try
            {
                testingEngine.Run();

                string assertionText = testingEngine.TestReport.GetText(config);
                assertionText +=
                    $"{Environment.NewLine} Random Generator Seed: " +
                    $"{testingEngine.TestReport.Configuration.RandomGeneratorSeed}{Environment.NewLine}";
                foreach (var bugReport in testingEngine.TestReport.BugReports)
                {
                    assertionText +=
                    $"{Environment.NewLine}" +
                    "Bug Report: " + bugReport.ToString(CultureInfo.InvariantCulture);
                }

                if (testingEngine.TestReport.NumOfFoundBugs > 0)
                {
                    var timeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ", CultureInfo.InvariantCulture);
                    var reproducibleTraceFileName = $"buggy-{timeStamp}.schedule";
                    assertionText += Environment.NewLine + "Reproducible trace which leads to the bug can be found at " +
                        $"{Path.Combine(Directory.GetCurrentDirectory(), reproducibleTraceFileName)}";

                    File.WriteAllText(reproducibleTraceFileName, testingEngine.ReproducibleTrace);
                }

                Assert.IsTrue(testingEngine.TestReport.NumOfFoundBugs == 0, assertionText);
            }
            finally
            {
                testingEngine.Stop();
            }
        }

        private static byte[] GetDogImageBytes() => new byte[] { 1, 2, 3 };

        private static byte[] GetCatImageBytes() => new byte[] { 4, 5, 6 };

        private static bool IsDogImage(byte[] imageBytes) => imageBytes.SequenceEqual(GetDogImageBytes());

        private static bool IsCatImage(byte[] imageBytes) => imageBytes.SequenceEqual(GetCatImageBytes());

        private static bool IsDogThumbnail(byte[] thumbnailBytes) => thumbnailBytes.SequenceEqual(GetDogImageBytes());

        private static bool IsCatThumbnail(byte[] thumbnailBytes) => thumbnailBytes.SequenceEqual(GetCatImageBytes());
    }
}
