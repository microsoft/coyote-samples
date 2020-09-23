// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ImageGallery.Store.Cosmos;

namespace ImageGallery.Models
{
    public class AccountEntity : CosmosEntity
    {
        public override string PartitionKey => Id;

        public string Name { get; set; }

        public string Email { get; set; }

        public AccountEntity(Account account)
        {
            this.Id = account.Id;
            this.Name = account.Name;
            this.Email = account.Email;
        }

        public Account GetAccount() =>
            new Account()
            {
                Id = this.Id,
                Name = this.Name,
                Email = this.Email
            };
    }
}
