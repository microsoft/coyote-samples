namespace PetImagesTest.MessagingMocks
{
    using Microsoft.Coyote.Specifications;
    using PetImages.Messaging;
    using PetImages.Storage;
    using PetImages.Worker;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class MockMessagingClient : IMessagingClient
    {
        private IWorker generateThumbnailWorker;

        public MockMessagingClient(IBlobContainer blobContainer)
        {
            generateThumbnailWorker = new GenerateThumbnailWorker(blobContainer);
        }

        public Task SubmitMessage(Message message)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (message.Type == Message.GenerateThumbnailMessageType)
                    {
                        var clonedMessage = TestHelper.Clone((GenerateThumbnailMessage)message);
                        await generateThumbnailWorker.ProcessMessage(clonedMessage);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                catch (Exception)
                {
                    Specification.Assert(false, "Uncaught exception in worker");
                }
            });

            return Task.CompletedTask;
        }
    }
}
