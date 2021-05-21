namespace PetImages.Messaging
{
    public class GenerateThumbnailMessage : Message
    {
        public GenerateThumbnailMessage()
        {
            Type = Message.GenerateThumbnailMessageType;
        }

        public string AccountName { get; set; }

        public string ImageStorageName { get; set; }
    }
}
