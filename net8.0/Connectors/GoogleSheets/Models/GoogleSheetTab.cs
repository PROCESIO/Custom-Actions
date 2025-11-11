using System.Text.Json.Serialization;

namespace GoogleSheetsAction.Models
{
    public class GoogleSheetTab
    {
        [JsonPropertyName("sheetId")]
        public int SheetId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }
}
