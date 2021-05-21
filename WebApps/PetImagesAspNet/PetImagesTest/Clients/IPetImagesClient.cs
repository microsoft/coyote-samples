// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using PetImages.Contracts;

namespace PetImagesTest.Clients
{
    public interface IPetImagesClient
    {
        public Task<ServiceResponse<Account>> CreateAccountAsync(Account account);
    }
}
