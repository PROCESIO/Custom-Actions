using System.Text.Json.Serialization;

namespace GoogleSheetConnector.Models
{
    public class GoogleDriveFile
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("webViewLink")]
        public string? WebViewLink { get; set; }
    }
}
