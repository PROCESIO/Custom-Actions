using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleSheetConnector.Models;

public sealed record GoogleSheetValueRange
{
    [JsonPropertyName("range")]
    public string? Range { get; init; }

    [JsonPropertyName("majorDimension")]
    public string? MajorDimension { get; init; }

    [JsonPropertyName("values")]
    public IReadOnlyList<IReadOnlyList<string>>? Values { get; init; }
}
