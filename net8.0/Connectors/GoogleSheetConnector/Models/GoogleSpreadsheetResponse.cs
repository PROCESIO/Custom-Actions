using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleSheetConnector.Models
{
    public class GoogleSpreadsheetResponse
    {
        [JsonPropertyName("spreadsheetId")]
        public string? SpreadsheetId { get; set; }

        [JsonPropertyName("sheets")]
        public List<GoogleSheet>? Sheets { get; set; }
    }
}
