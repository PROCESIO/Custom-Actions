using System.Text.Json.Serialization;

namespace GoogleSheetConnector.Models;

public sealed record GoogleSheet
{
    [JsonPropertyName("properties")]
    public GoogleSheetProperties? Properties { get; init; }
}
