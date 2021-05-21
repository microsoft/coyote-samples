namespace PetImages.Entities
{
    using System.Text.Json.Serialization;

    public abstract class DbItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        public abstract string PartitionKey { get; }
    }
}
