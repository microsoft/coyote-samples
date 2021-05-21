namespace PetImagesTest.StorageMocks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Text;
    using PetImages.Entities;

    using Database = System.Collections.Concurrent.ConcurrentDictionary<
        string, System.Collections.Concurrent.ConcurrentDictionary<string, PetImages.Entities.DbItem>>;

    using Container = System.Collections.Concurrent.ConcurrentDictionary<string, PetImages.Entities.DbItem>;
    using PetImages.Exceptions;
    using PetImages.Storage;

    public class MockCosmosState
    {
        private Database database = new Database();

        public void CreateContainer(string containerName)
        {
            EnsureContainerDoesNotExistInDatabase(containerName);

            _ = database.TryAdd(containerName, new Container());
        }

        public void GetContainer(string containerName)
        {
            EnsureContainerExistsInDatabase(containerName);
        }

        public void DeleteContainer(string containerName)
        {
            EnsureContainerExistsInDatabase(containerName);
        }

        public void CreateItem(string containerName, DbItem item)
        {
            EnsureItemDoesNotExistInDatabase(containerName, item.PartitionKey, item.Id);

            var container = database[containerName];
            _ = container.TryAdd(
                GetCombinedKey(item.PartitionKey, item.Id),
                item);
        }

        public void UpsertItem(string containerName, DbItem item)
        {
            EnsureContainerExistsInDatabase(containerName);

            var container = database[containerName];
            _ = container.TryAdd(
                GetCombinedKey(item.PartitionKey, item.Id),
                item);
        }

        public DbItem GetItem(string containerName, string partitionKey, string id)
        {
            EnsureItemExistsInDatabase(containerName, partitionKey, id);

            var container = database[containerName];
            _ = container.TryGetValue(GetCombinedKey(partitionKey, id), out DbItem item);
            return item;
        }

        public void DeleteItem(string containerName, string partitionKey, string id)
        {
            EnsureItemExistsInDatabase(containerName, partitionKey, id);

            var container = database[containerName];
            _ = container.TryRemove(GetCombinedKey(partitionKey, id), out DbItem _);
        }

        internal void EnsureContainerDoesNotExistInDatabase(string containerName)
        {
            if (database.ContainsKey(containerName))
            {
                throw new DatabaseContainerAlreadyExists();
            }
        }

        internal void EnsureContainerExistsInDatabase(string containerName)
        {
            if (!database.ContainsKey(containerName))
            {
                throw new DatabaseContainerDoesNotExist();
            }
        }

        internal void EnsureItemExistsInDatabase(string containerName, string partitionKey, string id)
        {
            EnsureContainerExistsInDatabase(containerName);
            var container = database[containerName];

            if (!container.ContainsKey(GetCombinedKey(partitionKey, id)))
            {
                throw new DatabaseItemDoesNotExistException();
            }
        }

        internal void EnsureItemDoesNotExistInDatabase(string containerName, string partitionKey, string id)
        {
            EnsureContainerExistsInDatabase(containerName);
            var container = database[containerName];

            if (container.ContainsKey(GetCombinedKey(partitionKey, id)))
            {
                throw new DatabaseItemAlreadyExistsException();
            }
        }

        internal static string GetCombinedKey(string partitionKey, string id)
        {
            return partitionKey + "_" + id;
        }
    }
}
