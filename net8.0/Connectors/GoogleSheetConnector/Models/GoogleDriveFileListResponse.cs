using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleSheetConnector.Models
{
    public class GoogleDriveFileListResponse
    {
        [JsonPropertyName("files")]
        public List<GoogleDriveFile>? Files { get; set; }

        [JsonPropertyName("nextPageToken")]
        public string? NextPageToken { get; set; }
    }
}
