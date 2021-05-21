namespace PetImagesTest.Clients
{
    using Microsoft.AspNetCore.Mvc;
    using PetImages.Contracts;
    using PetImages.Controllers;
    using PetImages.Messaging;
    using PetImages.Storage;
    using PetImagesTest.Exceptions;
    using System;
    using System.Net;
    using System.Threading.Tasks;

    public class TestPetImagesClient : IPetImagesClient
    {
        private ICosmosContainer accountContainer;
        private ICosmosContainer imageContainer;
        private IBlobContainer blobContainer;
        private IMessagingClient messagingClient;

        public TestPetImagesClient(ICosmosContainer accountContainer)
        {
            this.accountContainer = accountContainer;
        }

        public TestPetImagesClient(
            ICosmosContainer accountContainer,
            ICosmosContainer imageContainer,
            IBlobContainer blobContainer,
            IMessagingClient messagingClient)
        {
            this.accountContainer = accountContainer;
            this.imageContainer = imageContainer;
            this.blobContainer = blobContainer;
            this.messagingClient = messagingClient;
        }

        public TestPetImagesClient(ICosmosContainer accountContainer, ICosmosContainer imageContainer, IBlobContainer blobContainer)
        {
            this.accountContainer = accountContainer;
            this.imageContainer = imageContainer;
            this.blobContainer = blobContainer;
        }

        public async Task<ServiceResponse<Account>> CreateAccountAsync(Account account)
        {
            var accountCopy = TestHelper.Clone(account);

            return await Task.Run(async () =>
            {
                var controller = new AccountController(accountContainer);

                var actionResult = await InvokeControllerAction(async () => await controller.PutAsync(accountCopy));
                return ExtractServiceResponse<Account>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<Image>> CreateImageAsync(string accountName, Image image)
        {
            var imageCopy = TestHelper.Clone(image);

            return await Task.Run(async () =>
            {
                var controller = new ImageController(accountContainer, imageContainer, blobContainer, messagingClient);

                var actionResult = await InvokeControllerAction(async () => await controller.PutAsync(accountName, imageCopy));
                return ExtractServiceResponse<Image>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<Image>> CreateOrUpdateImageAsync(string accountName, Image image)
        {
            var imageCopy = TestHelper.Clone(image);

            return await Task.Run(async () =>
            {
                var controller = new ImageController(accountContainer, imageContainer, blobContainer, messagingClient);

                var actionResult = await InvokeControllerAction(async () => await controller.CreateOrUpdateAsync(accountName, imageCopy));
                return ExtractServiceResponse<Image>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<byte[]>> GetImageAsync(string accountName, string imageName)
        {
            return await Task.Run(async () =>
            {
                var controller = new ImageController(accountContainer, imageContainer, blobContainer, messagingClient);

                var actionResult = await InvokeControllerAction(async () => await controller.GetImageContentsAsync(accountName, imageName));
                return ExtractServiceResponse<byte[]>(actionResult.Result);
            });
        }

        public async Task<ServiceResponse<byte[]>> GetImageThumbnailAsync(string accountName, string imageName)
        {
            return await Task.Run(async () =>
            {
                var controller = new ImageController(accountContainer, imageContainer, blobContainer, messagingClient);

                var actionResult = await InvokeControllerAction(async () => await controller.GetImageThumbnailAsync(accountName, imageName));
                return ExtractServiceResponse<byte[]>(actionResult.Result);
            });
        }

        private static async Task<ActionResult<T>> InvokeControllerAction<T>(Func<Task<ActionResult<T>>> lambda)
        {
            // Simulate middleware by wrapping invocation of controller in exception handling
            // code which runs in middleware in production
            try
            {
                return await lambda();
            }
            catch (SimulatedDatabaseFaultException)
            {
                return new ActionResult<T>(new StatusCodeResult((int)HttpStatusCode.ServiceUnavailable));
            }
        }

        private static ServiceResponse<T> ExtractServiceResponse<T>(ActionResult<T> actionResult)
        {
            var response = actionResult.Result;

            if (response is OkObjectResult okObjectResult)
            {
                return new ServiceResponse<T>()
                {
                    StatusCode = (HttpStatusCode)okObjectResult.StatusCode,
                    Resource = (T)okObjectResult.Value
                };
            }
            else if (response is StatusCodeResult statusCodeResult)
            {
                return new ServiceResponse<T>()
                {
                    StatusCode = (HttpStatusCode)statusCodeResult.StatusCode
                };
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
