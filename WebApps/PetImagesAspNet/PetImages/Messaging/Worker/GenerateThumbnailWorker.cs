namespace PetImages.Worker
{
    using PetImages.Messaging;
    using PetImages.Storage;
    using System;
    using System.Threading.Tasks;

    public class GenerateThumbnailWorker : IWorker
    {
        private ICosmosContainer imageContainer;
        private IBlobContainer blobContainer;

        public GenerateThumbnailWorker(
            IBlobContainer imageBlobContainer)
        {
            this.blobContainer = imageBlobContainer;
        }

        public async Task ProcessMessage(Message message)
        {

            var thumbnailMessage = (GenerateThumbnailMessage)message;

            var accountName = thumbnailMessage.AccountName;
            var imageStorageName = thumbnailMessage.ImageStorageName;

            var imageContents = await blobContainer.GetBlobAsync(accountName, imageStorageName);

            var thumbnail = GenerateThumbnail(imageContents);

            var containerName = accountName + Constants.ThumbnailContainerNameSuffix;
            var blobName = imageStorageName + Constants.ThumbnailSuffix;

            await blobContainer.CreateContainerIfNotExistsAsync(containerName);
            await blobContainer.CreateOrUpdateBlobAsync(containerName, blobName, thumbnail);
        }

        private byte[] GenerateThumbnail(byte[] imageContents)
        {
            // Dummy implementation of GenerateThumbnail which returns the same bytes as the image
            return imageContents;
        }
    }
}
