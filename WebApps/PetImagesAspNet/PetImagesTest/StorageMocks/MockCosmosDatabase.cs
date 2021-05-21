namespace PetImagesTest.StorageMocks
{
    using PetImages.Entities;
    using PetImages.Storage;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    public class MockCosmosDatabase : ICosmosDatabase
    {
        private MockCosmosState state = new MockCosmosState();

        public MockCosmosDatabase(MockCosmosState state)
        {
            this.state = state;
        }

        public Task<ICosmosContainer> CreateContainerAsync(string containerName)
        {
            return Task.Run<ICosmosContainer>(() =>
            {
                state.CreateContainer(containerName);
                return new MockCosmosContainer(containerName, state);
            });
        }

        public Task<ICosmosContainer> GetContainer(string containerName)
        {
            return Task.Run<ICosmosContainer>(() =>
            {
                state.EnsureContainerExistsInDatabase(containerName);
                return new MockCosmosContainer(containerName, state);
            });
        }
    }
}
