// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading.Tasks;
using PetImages.Contracts;

#pragma warning disable SA1005
namespace PetImages.Tests.Clients
{
    public interface IPetImagesClient
    {
        public Task<HttpResponseMessage> CreateAccountAsync(Account account);

        //public Task<ServiceResponse<Image>> CreateImageAsync(string accountName, Image image);

        //public Task<ServiceResponse<Image>> CreateOrUpdateImageAsync(string accountName, Image image);

        //public Task<ServiceResponse<byte[]>> GetImageAsync(string accountName, string imageName);

        //public Task<ServiceResponse<byte[]>> GetImageThumbnailAsync(string accountName, string imageName);
    }
}
