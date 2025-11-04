using System.Text.Json.Serialization;

namespace GoogleSheetConnector.Models;

public sealed record GoogleDriveItem
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
