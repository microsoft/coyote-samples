namespace PetImages.Storage
{
    using PetImages.Entities;
    using System.Threading.Tasks;

    public interface ICosmosContainer
    {
        public Task<T> CreateItem<T>(T row)
            where T : DbItem;

        public Task<T> GetItem<T>(string partitionKey, string id)
           where T : DbItem;

        public Task<T> UpsertItem<T>(T row)
            where T : DbItem;

        public Task DeleteItem(string partitionKey, string id);
    }
}
