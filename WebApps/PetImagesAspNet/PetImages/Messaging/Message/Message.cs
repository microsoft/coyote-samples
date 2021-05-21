namespace PetImages.Messaging
{
    public abstract class Message
    {
        public const string GenerateThumbnailMessageType = "GenerateThumbnailMessage";

        public string Type { get; set; }
    }
}
