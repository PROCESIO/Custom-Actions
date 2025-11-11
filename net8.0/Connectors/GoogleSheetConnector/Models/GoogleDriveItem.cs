using System.Text.Json.Serialization;

namespace GoogleSheetConnector.Models
{
    public class GoogleDriveItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
