using System.Text.Json.Serialization;

namespace GoogleSheetsAction.Models;

public sealed record GoogleDriveFile
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("webViewLink")]
    public string? WebViewLink { get; init; }
}
