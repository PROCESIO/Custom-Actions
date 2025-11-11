using System.Text.Json.Serialization;

namespace GoogleSheetConnector.Models
{
    public class GoogleSheet
    {
        [JsonPropertyName("properties")]
        public GoogleSheetProperties? Properties { get; set; }
    }
}
