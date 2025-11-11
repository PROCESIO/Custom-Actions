using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleSheetConnector.Models
{
    public class GoogleSheetValueRange
    {
        [JsonPropertyName("range")]
        public string? Range { get; set; }

        [JsonPropertyName("majorDimension")]
        public string? MajorDimension { get; set; }

        [JsonPropertyName("values")]
        public List<List<string>>? Values { get; set; }
    }
}
