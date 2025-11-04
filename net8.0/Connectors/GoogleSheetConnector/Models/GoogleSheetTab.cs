using System.Text.Json.Serialization;

namespace GoogleSheetConnector.Models;

public sealed record GoogleSheetTab
{
    [JsonPropertyName("sheetId")]
    public int SheetId { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }
}
