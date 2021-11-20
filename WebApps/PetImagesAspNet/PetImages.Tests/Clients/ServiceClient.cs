// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using PetImages.Contracts;
using PetImages.Controllers;
using PetImages.Messaging;
using PetImages.Storage;
using PetImages.Tests.Exceptions;

#pragma warning disable SA1005
namespace PetImages.Tests
{
    internal class ServiceClient : IClient
    {
        private readonly HttpClient Client;

        internal ServiceClient(ServiceFactory factory)
        {
            this.Client = factory.CreateClient(new WebApplicationFactoryClientOptions()
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });
        }

        public async Task<HttpStatusCode> CreateAccountAsync(Account account)
        {
            var response = await this.Client.PostAsync(new Uri($"/api/account/create", UriKind.RelativeOrAbsolute),
                JsonContent.Create(account));
            return response.StatusCode;
        }

        public async Task<HttpStatusCode> CreateImageAsync(string accountName, Image image)
        {
            var response = await this.Client.PostAsync(new Uri($"/api/image/create/{accountName}",
                UriKind.RelativeOrAbsolute), JsonContent.Create(image));
            return response.StatusCode;
        }

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

        public async Task<(HttpStatusCode, byte[])> GetImageAsync(string accountName, string imageName)
        {
            var response = await this.Client.GetAsync(new Uri($"/api/image/contents/{accountName}/{imageName}",
                UriKind.RelativeOrAbsolute));
            var stream = await response.Content.ReadAsStreamAsync();
            byte[] content = response.StatusCode == HttpStatusCode.OK ?
                await JsonSerializer.DeserializeAsync<byte[]>(stream) : Array.Empty<byte>();
            return (response.StatusCode, content);
        }

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

        public void Dispose()
        {
            this.Client.Dispose();
        }
    }
}
