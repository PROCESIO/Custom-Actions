using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleSheetConnector.Models;

public sealed record GoogleDriveListResponse
{
    [JsonPropertyName("drives")]
    public IReadOnlyList<GoogleDriveItem>? Drives { get; init; }

    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; init; }
}
