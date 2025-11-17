using System.Text.Json.Serialization;

namespace GoogleSheetsAction.Models;

public sealed record GoogleDriveFileListResponse
{
    [JsonPropertyName("files")]
    public IReadOnlyList<GoogleDriveFile>? Files { get; init; }

    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; init; }
}
