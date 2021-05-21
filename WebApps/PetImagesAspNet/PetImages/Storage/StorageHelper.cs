namespace PetImages.Storage
{
    using PetImages.Entities;
    using PetImages.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public static class StorageHelper
    {
        public static async Task<bool> DoesItemExist<T>(ICosmosContainer container, string partitionKey, string id)
            where T : DbItem
        {
            try
            {
                await container.GetItem<T>(partitionKey, id);
                return true;
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return false;
            }
        }
    }
}
