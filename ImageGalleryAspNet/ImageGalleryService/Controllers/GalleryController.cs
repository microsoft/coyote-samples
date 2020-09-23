// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using ImageGallery.Models;
using ImageGallery.Store.AzureStorage;
using ImageGallery.Store.Cosmos;
using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.Controllers
{
    [ApiController]
    public class GalleryController : ControllerBase
    {
        private readonly IContainerProvider AccountContainer;
        private readonly IBlobContainerProvider StorageProvider;
        private readonly TextWriter Logger;

        public GalleryController(IDatabaseProvider databaseProvider, IBlobContainerProvider storageProvider, TextWriter logger)
        {
            this.AccountContainer = databaseProvider.GetContainer(Constants.AccountCollectionName);
            this.StorageProvider = storageProvider;
            this.Logger = logger;
        }

        [HttpPut]
        [Produces(typeof(ActionResult))]
        [Route("api/gallery/store")]
        public async Task<ActionResult> Store(Image image)
        {
            this.Logger.WriteLine("Storing image with name '{0}' and acccount id '{1}'.",
                image.Name, image.AccountId);

            // First, check if the account exists in Cosmos DB.
            var exists = await this.AccountContainer.ExistsItemAsync<AccountEntity>(image.AccountId, image.AccountId);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling the following APIs after checking if the account exists is racy and can
            // fail due to another concurrent request.

            // The account exists exists, so we can store the image to the blob storage.
            var containerName = Constants.GetContainerName(image.AccountId);
            await this.StorageProvider.CreateContainerIfNotExistsAsync(containerName);
            await this.StorageProvider.CreateBlobAsync(containerName, image.Name, image.Contents);
            return this.Ok();
        }

        [HttpGet]
        [Produces(typeof(ActionResult<Image>))]
        [Route("api/gallery/get/")]
        public async Task<ActionResult<Image>> Get(string accountId, string imageName)
        {
            this.Logger.WriteLine("Getting image with name '{0}' and acccount id '{1}'.",
                imageName, accountId);

            // First, check if the blob exists in Azure Storage.
            var containerName = Constants.GetContainerName(accountId);
            var exists = await this.StorageProvider.ExistsBlobAsync(containerName, imageName);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling get on the blob container after checking if the blob exists is racy and
            // can, for example, fail due to another concurrent request that deleted the blob.

            // The blob exists, so get the image.
            string contents = await this.StorageProvider.GetBlobAsync(containerName, imageName);
            return this.Ok(new Image(accountId, imageName, Encoding.Default.GetBytes(contents)));
        }

        [HttpDelete]
        [Produces(typeof(ActionResult))]
        [Route("api/gallery/delete/")]
        public async Task<ActionResult> Delete(string accountId, string imageName)
        {
            this.Logger.WriteLine("Deleting image with name '{0}' and acccount id '{1}'.",
                imageName, accountId);

            // First, check if the account exists in Cosmos DB.
            var exists = await this.AccountContainer.ExistsItemAsync<AccountEntity>(accountId, accountId);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling the following APIs after checking if the account exists is racy and can
            // fail due to another concurrent request.

            // The account exists, so check if the blob exists in Azure Storage.
            var containerName = Constants.GetContainerName(accountId);
            exists = await this.StorageProvider.ExistsBlobAsync(containerName, imageName);
            if (!exists)
            {
                return this.NotFound();
            }

            // The account exists, so delete the blob if it exists in Azure Storage.
            var deleted = await this.StorageProvider.DeleteBlobIfExistsAsync(containerName, imageName);
            if (!deleted)
            {
                return this.NotFound();
            }

            return this.Ok();
        }
    }
}
