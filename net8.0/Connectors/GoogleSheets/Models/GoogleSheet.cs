using System.Text.Json.Serialization;

namespace GoogleSheetsAction.Models;

public sealed record GoogleSheet
{
    [JsonPropertyName("properties")]
    public GoogleSheetProperties? Properties { get; init; }
}
