// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace ImageGallery.Store.AzureStorage
{
    /// <summary>
    /// Production implementation of an Azure Storage blob container provider.
    /// </summary>
    public class BlobContainerProvider : IBlobContainerProvider
    {
        private string ConnectionString;

        public BlobContainerProvider(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public async Task CreateContainerAsync(string containerName)
        {
            var blobContainerClient = new BlobContainerClient(this.ConnectionString, containerName);
            await blobContainerClient.CreateAsync();
        }

        public async Task CreateContainerIfNotExistsAsync(string containerName)
        {
            var blobContainerClient = new BlobContainerClient(this.ConnectionString, containerName);
            await blobContainerClient.CreateIfNotExistsAsync();
        }

        public async Task DeleteContainerAsync(string containerName)
        {
            var blobContainerClient = new BlobContainerClient(this.ConnectionString, containerName);
            await blobContainerClient.DeleteAsync();
        }

        public async Task<bool> DeleteContainerIfExistsAsync(string containerName)
        {
            var blobContainerClient = new BlobContainerClient(this.ConnectionString, containerName);
            var deleteInfo = await blobContainerClient.DeleteIfExistsAsync();
            return deleteInfo.Value;
        }

        public async Task CreateBlobAsync(string containerName, string blobName, byte[] blobContents)
        {
            var blobClient = new BlobClient(this.ConnectionString, containerName, blobName);
            await blobClient.UploadAsync(new MemoryStream(blobContents));
        }

        public async Task<string> GetBlobAsync(string containerName, string blobName)
        {
            var blobClient = new BlobClient(this.ConnectionString, containerName, blobName);
            var downloadInfo = await blobClient.DownloadAsync();
            return new StreamReader(downloadInfo.Value.Content).ReadToEnd();
        }

        public async Task<bool> ExistsBlobAsync(string containerName, string blobName)
        {
            var blobClient = new BlobClient(this.ConnectionString, containerName, blobName);
            var existsInfo = await blobClient.ExistsAsync();
            return existsInfo.Value;
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            var blobClient = new BlobClient(this.ConnectionString, containerName, blobName);
            await blobClient.DeleteAsync();
        }

        public async Task<bool> DeleteBlobIfExistsAsync(string containerName, string blobName)
        {
            var blobClient = new BlobClient(this.ConnectionString, containerName, blobName);
            var deleteInfo = await blobClient.DeleteIfExistsAsync();
            return deleteInfo.Value;
        }
    }
}
