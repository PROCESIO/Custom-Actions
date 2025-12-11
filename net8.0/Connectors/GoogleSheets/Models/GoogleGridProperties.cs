using System.Text.Json.Serialization;

namespace GoogleSheetsAction.Models;

public sealed record GoogleGridProperties
{
    [JsonPropertyName("rowCount")]
    public int RowCount { get; init; }

    [JsonPropertyName("columnCount")]
    public int ColumnCount { get; init; }

    [JsonPropertyName("frozenRowCount")]
    public int? FrozenRowCount { get; init; }

    [JsonPropertyName("frozenColumnCount")]
    public int? FrozenColumnCount { get; init; }
}
