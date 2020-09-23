// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ImageGallery.Logging;
using ImageGallery.Store.AzureStorage;

namespace ImageGallery.Tests.Mocks.AzureStorage
{
    /// <summary>
    /// Mock implementation of an Azure Storage blob container provider.
    /// </summary>
    internal class MockBlobContainerProvider : IBlobContainerProvider
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>> Containers;
        private readonly MockLogger Logger;

        internal MockBlobContainerProvider(MockLogger logger)
        {
            this.Containers = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>>();
            this.Logger = logger;
        }

        public async Task CreateContainerAsync(string containerName)
        {
            // Used to model asynchrony in the request.
            await Task.Yield();

            this.Logger.WriteLine("Creating container '{0}'.", containerName);
            this.Containers.TryAdd(containerName, new ConcurrentDictionary<string, byte[]>());
        }

        public async Task CreateContainerIfNotExistsAsync(string containerName)
        {
            await Task.Yield();

            this.Logger.WriteLine("Creating container '{0}' if it does not exist.", containerName);
            this.Containers.TryAdd(containerName, new ConcurrentDictionary<string, byte[]>());
        }

        public async Task DeleteContainerAsync(string containerName)
        {
            await Task.Yield();

            this.Logger.WriteLine("Deleting container '{0}'.", containerName);
            this.Containers.TryRemove(containerName, out ConcurrentDictionary<string, byte[]> _);
        }

        public async Task<bool> DeleteContainerIfExistsAsync(string containerName)
        {
            await Task.Yield();

            this.Logger.WriteLine("Deleting container '{0}' if it exists.", containerName);
            return this.Containers.TryRemove(containerName, out ConcurrentDictionary<string, byte[]> _);
        }

        public async Task CreateBlobAsync(string containerName, string blobName, byte[] blobContents)
        {
            await Task.Yield();

            this.Logger.WriteLine("Creating blob '{0}' in container '{1}'.", blobName, containerName);
            this.Containers[containerName].TryAdd(blobName, blobContents);
        }

        public async Task<string> GetBlobAsync(string containerName, string blobName)
        {
            await Task.Yield();

            this.Logger.WriteLine("Getting blob '{0}' from container '{1}'.", blobName, containerName);
            return Encoding.Default.GetString(this.Containers[containerName][blobName]);
        }

        public async Task<bool> ExistsBlobAsync(string containerName, string blobName)
        {
            await Task.Yield();

            this.Logger.WriteLine("Checking if blob '{0}' exists in container '{1}'.", blobName, containerName);
            return this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container) &&
                container.ContainsKey(blobName);
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            await Task.Yield();

            this.Logger.WriteLine("Deleting blob '{0}' from container '{1}'.", blobName, containerName);
            this.Containers[containerName].TryRemove(blobName, out byte[] _);
        }

        public async Task<bool> DeleteBlobIfExistsAsync(string containerName, string blobName)
        {
            await Task.Yield();

            this.Logger.WriteLine("Deleting blob '{0}' from container '{1}' if it exists.", blobName, containerName);
            if (!this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container))
            {
                return false;
            }

            return container.TryRemove(blobName, out byte[] _);
        }
    }
}
