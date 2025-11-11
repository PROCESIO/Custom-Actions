using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleSheetsAction.Models
{
    public class GoogleDriveListResponse
    {
        [JsonPropertyName("drives")]
        public List<GoogleDriveItem>? Drives { get; set; }

        [JsonPropertyName("nextPageToken")]
        public string? NextPageToken { get; set; }
    }
}
