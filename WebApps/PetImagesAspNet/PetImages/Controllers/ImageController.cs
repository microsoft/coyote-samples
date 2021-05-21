namespace PetImages.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using PetImages.Contracts;
    using PetImages.Entities;
    using PetImages.Exceptions;
    using PetImages.Messaging;
    using PetImages.Storage;
    using System;
    using System.Threading.Tasks;

    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {
        private ICosmosContainer accountContainer;
        private ICosmosContainer imageContainer;
        private IBlobContainer blobContainer;
        private IMessagingClient messagingClient;

        public ImageController(
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

        // Scenario 2 - Buggy PutAsync version
        [HttpPost]
        public async Task<ActionResult<Image>> PutAsync(string accountName, Image image)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(accountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            var imageItem = image.ToItem();

            await blobContainer.CreateContainerIfNotExistsAsync(accountName);
            await blobContainer.CreateOrUpdateBlobAsync(accountName, image.Name, image.Content);

            try
            {
                imageItem = await imageContainer.CreateItem(imageItem);
            }
            catch (DatabaseItemAlreadyExistsException)
            {
                return this.Conflict();
            }
            catch (DatabaseException) // some, possibly intermittent exception thrown by cosmos db layer
            {
                await blobContainer.DeleteBlobIfExistsAsync(accountName, image.Name);
                return this.StatusCode(503);
            }

            return this.Ok(imageItem.ToImage());
        }

        // Scenario 2 - Fixed PutAsync version
        public async Task<ActionResult<Image>> PutAsyncFixed(string accountName, Image image)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(accountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            var imageItem = image.ToItem();

            await blobContainer.CreateContainerIfNotExistsAsync(accountName);
            await blobContainer.CreateOrUpdateBlobAsync(accountName, image.Name, image.Content);

            try
            {
                imageItem = await imageContainer.CreateItem(imageItem);
            }
            catch (DatabaseItemAlreadyExistsException)
            {
                return this.Conflict();
            }

            // We don't delete the blob in the controller; orphaned blobs (i.e. blobs with no corresponding
            // cosmos db entry) are cleaned up asynchronously by a background "garbage collector" worker (not
            // shown in this sample)

            return this.Ok(imageItem.ToImage());
        }

        [HttpGet]
        public async Task<ActionResult<byte[]>> GetImageContentsAsync(string accountName, string imageName)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(accountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            ImageItem imageItem;
            try
            {
                imageItem = await this.imageContainer.GetItem<ImageItem>(partitionKey: imageName, id: imageName);
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NotFound();
            }

            if (!await blobContainer.ExistsBlobAsync(accountName, imageItem.StorageName))
            {
                return this.NotFound();
            }

            return this.Ok(await blobContainer.GetBlobAsync(accountName, imageItem.StorageName));
        }

        [HttpGet]
        public async Task<ActionResult<byte[]>> GetImageThumbnailAsync(string accountName, string imageName)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(accountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            ImageItem imageItem;
            try
            {
                imageItem = await this.imageContainer.GetItem<ImageItem>(partitionKey: imageName, id: imageName);
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NotFound();
            }

            var containerName = accountName + Constants.ThumbnailContainerNameSuffix;
            var blobName = imageItem.StorageName + Constants.ThumbnailSuffix;

            if (!await blobContainer.ExistsBlobAsync(containerName, blobName))
            {
                return this.NotFound();
            }

            return this.Ok(await blobContainer.GetBlobAsync(containerName, blobName));
        }

        // Scenario 3 - Buggy CreateOrUpdateAsync version
        [HttpPut]
        public async Task<ActionResult<Image>> CreateOrUpdateAsync(string accountName, Image image)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(accountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            var imageItem = image.ToItem();

            await blobContainer.CreateContainerIfNotExistsAsync(accountName);
            await blobContainer.CreateOrUpdateBlobAsync(accountName, image.Name, image.Content);

            imageItem = await imageContainer.UpsertItem(imageItem);

            await messagingClient.SubmitMessage(new GenerateThumbnailMessage()
            {
                AccountName = accountName,
                ImageStorageName = image.Name
            });

            return this.Ok(imageItem.ToImage());
        }

        // Scenario 3 - Fixed CreateOrUpdateAsync version
        public async Task<ActionResult<Image>> CreateOrUpdateAsyncFixed(string accountName, Image image)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(accountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            var imageItem = image.ToItem();

            var uniqueId = Guid.NewGuid().ToString();
            imageItem.StorageName = uniqueId;

            await blobContainer.CreateContainerIfNotExistsAsync(accountName);
            await blobContainer.CreateOrUpdateBlobAsync(accountName, imageItem.StorageName, image.Content);

            imageItem = await imageContainer.UpsertItem(imageItem);

            await messagingClient.SubmitMessage(new GenerateThumbnailMessage()
            {
                AccountName = accountName,
                ImageStorageName = imageItem.StorageName
            });

            return this.Ok(imageItem.ToImage());
        }
    }
}
