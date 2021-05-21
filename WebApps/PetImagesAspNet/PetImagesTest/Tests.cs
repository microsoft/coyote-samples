
namespace PetImagesTest
{
    using Microsoft.Coyote;
    using NUnit.Framework;
    using PetImages;
    using PetImages.Contracts;
    using PetImages.Controllers;
    using PetImagesTest.Clients;
    using PetImagesTest.MessagingMocks;
    using PetImagesTest.StorageMocks;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    public class Tests
    {
        [Test]
        public async Task TestFirstScenario()
        {
            // Initialize the mock in-memory DB and account manager.
            var cosmosState = new MockCosmosState();
            var database = new MockCosmosDatabase(cosmosState);
            var accountContainer = await database.CreateContainerAsync(Constants.AccountContainerName);
            var petImagesClient = new TestPetImagesClient(accountContainer);

            // Create an account request payload
            var account = new Account()
            {
                Name = "MyAccount"
            };

            // Call CreateAccount twice without awaiting, which makes both methods run
            // asynchronously with each other.
            var task1 = petImagesClient.CreateAccountAsync(account);
            var task2 = petImagesClient.CreateAccountAsync(account);

            // Then wait both requests to complete.
            await Task.WhenAll(task1, task2);

            var statusCode1 = task1.Result.StatusCode;
            var statusCode2 = task2.Result.StatusCode;

            // Finally, assert that only one of the two requests succeeded and the other
            // failed. Note that we do not know which one of the two succeeded as the
            // requests ran concurrently (this is why we use an exclusive OR).
            Assert.True(
                (statusCode1 == HttpStatusCode.OK && statusCode2 == HttpStatusCode.Conflict) ||
                (statusCode1 == HttpStatusCode.Conflict && statusCode2 == HttpStatusCode.OK));
        }

        [Test]
        public async Task TestSecondScenario()
        {
            var cosmosState = new MockCosmosState();
            var database = new MockCosmosDatabase(cosmosState);
            var accountContainer = (MockCosmosContainer)await database.CreateContainerAsync(Constants.AccountContainerName);
            var imageContainer = (MockCosmosContainer)await database.CreateContainerAsync(Constants.ImageContainerName);
            var blobContainer = new MockBlobContainerProvider();
            var messagingClient = new MockMessagingClient(blobContainer);

            var petImagesClient = new TestPetImagesClient(accountContainer, imageContainer, blobContainer, messagingClient);

            string accountName = "MyAccount";
            string imageName = "pet.jpg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName
            };

            var accountResult = await petImagesClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            imageContainer.EnableRandomizedFaults();

            var task1 = petImagesClient.CreateImageAsync(accountName, new Image() { Name = imageName, Content = GetDogImageBytes() });
            var task2 = petImagesClient.CreateImageAsync(accountName, new Image() { Name = imageName, Content = GetDogImageBytes() });
            await Task.WhenAll(task1, task2);

            var statusCode1 = task1.Result.StatusCode;
            var statusCode2 = task2.Result.StatusCode;

            imageContainer.DisableRandomizedFaults();

            Assert.IsTrue(statusCode1 == HttpStatusCode.OK || statusCode1 == HttpStatusCode.Conflict || statusCode1 == HttpStatusCode.ServiceUnavailable);
            Assert.IsTrue(statusCode2 == HttpStatusCode.OK || statusCode2 == HttpStatusCode.Conflict || statusCode2 == HttpStatusCode.ServiceUnavailable);

            if (task1.Result.StatusCode == HttpStatusCode.OK || task2.Result.StatusCode == HttpStatusCode.OK)
            {
                var imageContentResult = await petImagesClient.GetImageAsync(accountName, imageName);
                Assert.IsTrue(imageContentResult.StatusCode == HttpStatusCode.OK);
                Assert.IsTrue(IsDogImage(imageContentResult.Resource));
            }
        }

        [Test]
        public async Task TestThirdScenario()
        {
            var cosmosState = new MockCosmosState();
            var database = new MockCosmosDatabase(cosmosState);
            var accountContainer = (MockCosmosContainer)await database.CreateContainerAsync(Constants.AccountContainerName);
            var imageContainer = (MockCosmosContainer)await database.CreateContainerAsync(Constants.ImageContainerName);
            var blobContainer = new MockBlobContainerProvider();
            var messagingClient = new MockMessagingClient(blobContainer);

            var petImagesClient = new TestPetImagesClient(accountContainer, imageContainer, blobContainer, messagingClient);

            string accountName = "MyAccount";
            string imageName = "pet.jpg";

            // Create an account request payload
            var account = new Account()
            {
                Name = accountName
            };

            var accountResult = await petImagesClient.CreateAccountAsync(account);
            Assert.IsTrue(accountResult.StatusCode == HttpStatusCode.OK);

            var task1 = petImagesClient.CreateOrUpdateImageAsync(accountName, new Image() { Name = imageName, Content = GetDogImageBytes() });
            var task2 = petImagesClient.CreateOrUpdateImageAsync(accountName, new Image() { Name = imageName, Content = GetCatImageBytes() });
            await Task.WhenAll(task1, task2);

            Assert.IsTrue(task1.Result.StatusCode == HttpStatusCode.OK);
            Assert.IsTrue(task1.Result.StatusCode == HttpStatusCode.OK);

            var imageResult = await petImagesClient.GetImageAsync(accountName, imageName);
            Assert.IsTrue(imageResult.StatusCode == HttpStatusCode.OK);
            byte[] image = imageResult.Resource;

            byte[] thumbnail;
            while (true)
            {
                var thumbnailResult = await petImagesClient.GetImageThumbnailAsync(accountName, imageName);
                if (thumbnailResult.StatusCode == HttpStatusCode.OK)
                {
                    thumbnail = thumbnailResult.Resource;
                    break;
                }
            }

            Assert.IsTrue(
                (IsDogImage(image) && IsDogThumbnail(thumbnail)) ||
                (IsCatImage(image) && IsCatThumbnail(thumbnail)));
        }

        [Test]
        public void SystematicTestFirstScenario()
        {
            RunCoyoteTest(TestFirstScenario);
        }

        [Test]
        public void SystematicTestSecondScenario()
        {
            RunCoyoteTest(TestSecondScenario);
        }

        [Test]
        public void SystematicTestThirdScenario()
        {
            RunCoyoteTest(TestThirdScenario);
        }


        private static void RunCoyoteTest(Func<Task> test, string reproducibleScheduleFilePath = null)
        {
            var config = Configuration
                .Create()
                .WithTestingIterations(100);

            if (reproducibleScheduleFilePath != null)
            {
                var trace = File.ReadAllText(reproducibleScheduleFilePath);
                config = config.WithReplayStrategy(trace);
            }

            var testingEngine = Microsoft.Coyote.SystematicTesting.TestingEngine.Create(config, test);

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
                    var reproducibleTraceFileName = $"buggy.schedule.{timeStamp}";
                    assertionText +=
                        Environment.NewLine +
                        $"Reproducible schedule which lead to the bug can be found at {Path.Combine(Directory.GetCurrentDirectory(), reproducibleTraceFileName)}";

                    File.WriteAllText(reproducibleTraceFileName, testingEngine.ReproducibleTrace);
                }

                Assert.IsTrue(testingEngine.TestReport.NumOfFoundBugs == 0, assertionText);
            }
            finally
            {
                testingEngine.Stop();
            }
        }

        private static byte[] GetDogImageBytes()
        {
            return new byte[] { 1, 2, 3 };
        }

        private static byte[] GetCatImageBytes()
        {
            return new byte[] { 4, 5, 6 };
        }

        private static bool IsDogImage(byte[] imageBytes)
        {
            return imageBytes.SequenceEqual(GetDogImageBytes());
        }

        private static bool IsCatImage(byte[] imageBytes)
        {
            return imageBytes.SequenceEqual(GetCatImageBytes());
        }

        private static bool IsDogThumbnail(byte[] thumbnailBytes)
        {
            return thumbnailBytes.SequenceEqual(GetDogImageBytes());
        }

        private static bool IsCatThumbnail(byte[] thumbnailBytes)
        {
            return thumbnailBytes.SequenceEqual(GetCatImageBytes());
        }
    }
}
