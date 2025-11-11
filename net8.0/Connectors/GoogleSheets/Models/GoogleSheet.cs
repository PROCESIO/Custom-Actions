using System.Text.Json.Serialization;

namespace GoogleSheetsAction.Models
{
    public class GoogleSheet
    {
        [JsonPropertyName("properties")]
        public GoogleSheetProperties? Properties { get; set; }
    }
}
