namespace PetImages.Storage
{
    using PetImages.Entities;
    using System.Threading.Tasks;

    public interface ICosmosDatabase
    {
        Task<ICosmosContainer> CreateContainerAsync(string containerName);

        Task<ICosmosContainer> GetContainer(string containerName);
    }
}
