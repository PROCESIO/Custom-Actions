using System.Text.Json.Serialization;

namespace GoogleSheetsAction.Models;

public sealed record GoogleSheetProperties
{
    [JsonPropertyName("sheetId")]
    public int SheetId { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }
}
