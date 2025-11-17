using System.Text.Json.Serialization;

namespace GoogleSheetsAction.Models;

public sealed record GoogleSpreadsheetResponse
{
    [JsonPropertyName("spreadsheetId")]
    public string? SpreadsheetId { get; init; }

    [JsonPropertyName("sheets")]
    public IReadOnlyList<GoogleSheet>? Sheets { get; init; }
}
