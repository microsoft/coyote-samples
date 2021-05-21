namespace PetImages.Worker
{
    using PetImages.Messaging;
    using System.Threading.Tasks;

    public interface IWorker
    {
        Task ProcessMessage(Message message);
    }
}
