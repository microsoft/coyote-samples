﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Entities;
using PetImages.Exceptions;
using PetImages.Messaging;
using PetImages.Storage;

namespace PetImages.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly ICosmosContainer AccountContainer;
        private readonly ICosmosContainer ImageContainer;
        private readonly IBlobContainer BlobContainer;
        private readonly IMessagingClient MessagingClient;

        public ImageController(ICosmosContainer accountContainer, ICosmosContainer imageContainer,
            IBlobContainer blobContainer, IMessagingClient messagingClient)
        {
            this.AccountContainer = accountContainer;
            this.ImageContainer = imageContainer;
            this.BlobContainer = blobContainer;
            this.MessagingClient = messagingClient;
        }

        /// <summary>
        /// Scenario 2 - Buggy CreateImageAsync version.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Image>> CreateImageAsync(string accountName, Image image)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(this.AccountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            var imageItem = image.ToItem();

            // We upload the image to Azure Storage, before adding an entry to Cosmos DB
            // so that it is guaranteed to be there when user does a GET request.
            // Note: we're calling CreateOrUpdateBlobAsync because Azure Storage doesn't
            // have a create-only API.
            await this.BlobContainer.CreateContainerIfNotExistsAsync(accountName);
            await this.BlobContainer.CreateOrUpdateBlobAsync(accountName, image.Name, image.Content);

            try
            {
                imageItem = await this.ImageContainer.CreateItem(imageItem);
            }
            catch (DatabaseItemAlreadyExistsException)
            {
                return this.Conflict();
            }
            catch (DatabaseException)
            {
                // We handle an exception thrown by Cosmos DB layer, perhaps due to some
                // intermittent failure, by cleaning up the image to not waste resources.
                await this.BlobContainer.DeleteBlobIfExistsAsync(accountName, image.Name);
                return this.StatusCode(503);
            }

            return this.Ok(imageItem.ToImage());
        }

        /// <summary>
        /// Scenario 2 - Fixed CreateImageAsync version.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Image>> CreateImageAsyncFixed(string accountName, Image image)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(this.AccountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            var imageItem = image.ToItem();

            await this.BlobContainer.CreateContainerIfNotExistsAsync(accountName);
            await this.BlobContainer.CreateOrUpdateBlobAsync(accountName, image.Name, image.Content);

            try
            {
                imageItem = await this.ImageContainer.CreateItem(imageItem);
            }
            catch (DatabaseItemAlreadyExistsException)
            {
                return this.Conflict();
            }

            // We don't delete the blob in the controller; orphaned blobs (i.e., blobs with no corresponding
            // Cosmos DB entry) are cleaned up asynchronously by a background "garbage collector" worker
            // (not shown in this sample).

            return this.Ok(imageItem.ToImage());
        }

        [HttpGet]
        public async Task<ActionResult<byte[]>> GetImageContentsAsync(string accountName, string imageName)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(this.AccountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            ImageItem imageItem;
            try
            {
                imageItem = await this.ImageContainer.GetItem<ImageItem>(partitionKey: imageName, id: imageName);
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NotFound();
            }

            if (!await this.BlobContainer.ExistsBlobAsync(accountName, imageItem.StorageName))
            {
                return this.NotFound();
            }

            return this.Ok(await this.BlobContainer.GetBlobAsync(accountName, imageItem.StorageName));
        }

        [HttpGet]
        public async Task<ActionResult<byte[]>> GetImageThumbnailAsync(string accountName, string imageName)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(this.AccountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            ImageItem imageItem;
            try
            {
                imageItem = await this.ImageContainer.GetItem<ImageItem>(partitionKey: imageName, id: imageName);
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return this.NotFound();
            }

            var containerName = accountName + Constants.ThumbnailContainerNameSuffix;
            var blobName = imageItem.StorageName + Constants.ThumbnailSuffix;

            if (!await this.BlobContainer.ExistsBlobAsync(containerName, blobName))
            {
                return this.NotFound();
            }

            return this.Ok(await this.BlobContainer.GetBlobAsync(containerName, blobName));
        }

        /// <summary>
        /// Scenario 3 - Buggy CreateOrUpdateImageAsync version.
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<Image>> CreateOrUpdateImageAsync(string accountName, Image image)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(this.AccountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            var imageItem = image.ToItem();

            await this.BlobContainer.CreateContainerIfNotExistsAsync(accountName);
            await this.BlobContainer.CreateOrUpdateBlobAsync(accountName, image.Name, image.Content);

            imageItem = await this.ImageContainer.UpsertItem(imageItem);

            await this.MessagingClient.SubmitMessage(new GenerateThumbnailMessage()
            {
                AccountName = accountName,
                ImageStorageName = image.Name
            });

            return this.Ok(imageItem.ToImage());
        }

        /// <summary>
        /// Scenario 3 - Fixed CreateOrUpdateImageAsync version.
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<Image>> CreateOrUpdateImageAsyncFixed(string accountName, Image image)
        {
            if (!await StorageHelper.DoesItemExist<AccountItem>(this.AccountContainer, partitionKey: accountName, id: accountName))
            {
                return this.NotFound();
            }

            var imageItem = image.ToItem();

            var uniqueId = Guid.NewGuid().ToString();
            imageItem.StorageName = uniqueId;

            await this.BlobContainer.CreateContainerIfNotExistsAsync(accountName);
            await this.BlobContainer.CreateOrUpdateBlobAsync(accountName, imageItem.StorageName, image.Content);

            imageItem = await this.ImageContainer.UpsertItem(imageItem);

            await this.MessagingClient.SubmitMessage(new GenerateThumbnailMessage()
            {
                AccountName = accountName,
                ImageStorageName = imageItem.StorageName
            });

            return this.Ok(imageItem.ToImage());
        }
    }
}
