namespace PetImagesTest.StorageMocks
{
    using Microsoft.Coyote.Random;
    using PetImages.Entities;
    using PetImages.Exceptions;
    using PetImages.Storage;
    using PetImagesTest.Exceptions;
    using System.Collections.Concurrent;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class MockCosmosContainer : ICosmosContainer
    {
        private string containerName;
        private MockCosmosState state = new MockCosmosState();
        private bool emitRandomizedFaults = false;
        private Generator generator = Generator.Create();

        public MockCosmosContainer(string containerName, MockCosmosState state)
        {
            this.containerName = containerName;
            this.state = state;
        }

        public Task<T> CreateItem<T>(T item)
            where T : DbItem
        {
            var itemCopy = TestHelper.Clone(item);

            return Task.Run(() =>
            {
                if (emitRandomizedFaults && generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                state.CreateItem(containerName, itemCopy);
                return itemCopy;
            });
        }

        public Task<T> GetItem<T>(string partitionKey, string id)
            where T : DbItem
        {
            return Task.Run(() =>
            {
                if (emitRandomizedFaults && generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                var item = state.GetItem(containerName, partitionKey, id);

                var itemCopy = TestHelper.Clone((T)item);

                return itemCopy;
            });
        }

        public Task<T> UpsertItem<T>(T item)
            where T : DbItem
        {
            return Task.Run(() =>
            {
                if (emitRandomizedFaults && generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                var itemCopy = TestHelper.Clone(item);
                state.UpsertItem(containerName, itemCopy);
                return itemCopy;
            });
        }

        public Task DeleteItem(string partitionKey, string id)
        {
            return Task.Run(() =>
            {
                if (emitRandomizedFaults && generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                state.DeleteItem(containerName, partitionKey, id);
            });
        }

        public void EnableRandomizedFaults()
        {
            emitRandomizedFaults = true;
        }

        public void DisableRandomizedFaults()
        {
            emitRandomizedFaults = false;
        }

    }
}
