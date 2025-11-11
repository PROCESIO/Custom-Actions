using System.Text.Json.Serialization;

namespace GoogleSheetsAction.Models
{
    public class GoogleDriveItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
