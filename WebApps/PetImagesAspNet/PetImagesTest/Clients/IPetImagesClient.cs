namespace PetImagesTest.Clients
{
    using PetImages.Contracts;
    using System.Threading.Tasks;

    public interface IPetImagesClient
    {
        public Task<ServiceResponse<Account>> CreateAccountAsync(Account account);
    }
}
