// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Controllers;
using PetImages.Messaging;
using PetImages.Storage;
using PetImages.Tests.Exceptions;

#pragma warning disable SA1005
namespace PetImages.Tests.Clients
{
    public class TestPetImagesClient : IPetImagesClient
    {
        private readonly HttpClient Client;

        public TestPetImagesClient(HttpClient client)
        {
            this.Client = client;
        }

        public async Task<HttpResponseMessage> CreateAccountAsync(Account account)
        {
            var content = JsonContent.Create(account);
            return await this.Client.PostAsync(new Uri($"/api/account/create", UriKind.RelativeOrAbsolute), content);
        }

        // public async Task<ServiceResponse<Image>> CreateImageAsync(string accountName, Image image)
        // {
        //     var imageCopy = TestHelper.Clone(image);

        //     return await Task.Run(async () =>
        //     {
        //         var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
        //         var actionResult = await InvokeControllerAction(async () => await controller.CreateImageAsync(accountName, imageCopy));
        //         return ExtractServiceResponse<Image>(actionResult.Result);
        //     });
        // }

        // public async Task<ServiceResponse<Image>> CreateOrUpdateImageAsync(string accountName, Image image)
        // {
        //     var imageCopy = TestHelper.Clone(image);

        //     return await Task.Run(async () =>
        //     {
        //         var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
        //         var actionResult = await InvokeControllerAction(async () => await controller.CreateOrUpdateImageAsync(accountName, imageCopy));
        //         return ExtractServiceResponse<Image>(actionResult.Result);
        //     });
        // }

        // public async Task<ServiceResponse<byte[]>> GetImageAsync(string accountName, string imageName)
        // {
        //     return await Task.Run(async () =>
        //     {
        //         var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
        //         var actionResult = await InvokeControllerAction(async () => await controller.GetImageContentsAsync(accountName, imageName));
        //         return ExtractServiceResponse<byte[]>(actionResult.Result);
        //     });
        // }

        // public async Task<ServiceResponse<byte[]>> GetImageThumbnailAsync(string accountName, string imageName)
        // {
        //     return await Task.Run(async () =>
        //     {
        //         var controller = new ImageController(this.AccountContainer, this.ImageContainer, this.BlobContainer, this.MessagingClient);
        //         var actionResult = await InvokeControllerAction(async () => await controller.GetImageThumbnailAsync(accountName, imageName));
        //         return ExtractServiceResponse<byte[]>(actionResult.Result);
        //     });
        // }

        // /// <summary>
        // /// Simulate middleware by wrapping invocation of controller in exception handling
        // /// code which runs in middleware in production.
        // /// </summary>
        // private static async Task<ActionResult<T>> InvokeControllerAction<T>(Func<Task<ActionResult<T>>> lambda)
        // {
        //     try
        //     {
        //         return await lambda();
        //     }
        //     catch (SimulatedDatabaseFaultException)
        //     {
        //         return new ActionResult<T>(new StatusCodeResult((int)HttpStatusCode.ServiceUnavailable));
        //     }
        // }

        // private static ServiceResponse<T> ExtractServiceResponse<T>(ActionResult<T> actionResult)
        // {
        //     var response = actionResult.Result;
        //     if (response is OkObjectResult okObjectResult)
        //     {
        //         return new ServiceResponse<T>()
        //         {
        //             StatusCode = (HttpStatusCode)okObjectResult.StatusCode,
        //             Resource = (T)okObjectResult.Value
        //         };
        //     }
        //     else if (response is StatusCodeResult statusCodeResult)
        //     {
        //         return new ServiceResponse<T>()
        //         {
        //             StatusCode = (HttpStatusCode)statusCodeResult.StatusCode
        //         };
        //     }
        //     else
        //     {
        //         throw new InvalidOperationException();
        //     }
        // }
    }
}
