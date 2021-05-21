namespace PetImages.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public interface IMessagingClient
    {
        Task SubmitMessage(Message message);
    }
}
