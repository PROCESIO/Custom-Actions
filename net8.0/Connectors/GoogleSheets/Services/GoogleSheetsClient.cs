using GoogleSheetsAction.Models;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;
using System.Globalization;
using System.Text.Json;

namespace GoogleSheetsAction.Services;

public sealed class GoogleSheetsClient
{
    private readonly APICredentialsManager _credentials;

    public GoogleSheetsClient(APICredentialsManager? credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        if (_credentials.Client is null)
        {
            throw new ArgumentException("Credentials client is not configured.", nameof(credentials));
        }
    }

    public async Task<GoogleSpreadsheetResponse?> GetSpreadsheetAsync(string? spreadsheetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);

        var response = await _credentials.Client.GetAsync($"v4/spreadsheets/{spreadsheetId}", new(), new());
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GoogleSpreadsheetResponse>(payload);
    }

    public async Task<string> CreateSpreadSheetAsync(string? spreadSheetTitle)
    {
        var title = spreadSheetTitle?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new Exception("Spreadsheet title is required.");
        }

        var request = new
        {
            properties = new
            {
                title
            }
        };

        HttpResponseMessage createResponse;
        string createPayload;
        try
        {
            createResponse = await _credentials.Client.PostAsync("v4/spreadsheets", null, null, request);
            createPayload = await createResponse.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"CreateSpreadsheetFailed  exception message : {ex.Message}");
        }

        if (!createResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Google Sheets API responded with status {(int)createResponse.StatusCode} {createResponse.StatusCode}. Content: {createPayload}");
        }

        return createPayload;
    }

    public async Task UpdateHeadersAsync(
        string? defaultSheetTitle,
        string spreadSheetId,
        IList<string> headerValues)
    {
        var targetSheet = string.IsNullOrWhiteSpace(defaultSheetTitle) ? "Sheet1" : defaultSheetTitle;
        var range = $"{targetSheet}!1:1";
        var updateQuery = new Dictionary<string, string>
        {
            ["valueInputOption"] = "RAW"
        };

        var updateBody = new
        {
            values = new List<IList<string>> { headerValues }
        };

        var updateResponse = await _credentials.Client.PutAsync($"v4/spreadsheets/{spreadSheetId}/values/{Uri.EscapeDataString(range)}", updateQuery, null, updateBody);

        if (!updateResponse.IsSuccessStatusCode)
        {
            var updatePayload = await updateResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to apply headers to the new spreadsheet. Status {(int)updateResponse.StatusCode} {updateResponse.StatusCode}. Content: {updatePayload}");
        }
    }

    public async Task<GoogleSheetValueRange?> GetSheetValuesAsync(string? spreadsheetId, string? sheetName, string range = "A:Z")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);

        var relativeRange = string.IsNullOrEmpty(range) ? sheetName : $"{sheetName}!{range}";
        var response = await _credentials.Client.GetAsync($"v4/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(relativeRange)}", new(), new());
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GoogleSheetValueRange>(payload);
    }

    public async Task<IReadOnlyList<OptionModel>> BuildRowNumberOptionsAsync(string spreadsheetId, string sheetName)
    {
        var values = await GetSheetValuesAsync(spreadsheetId, sheetName, "A:Z");
        var result = new List<OptionModel>();
        if (values?.Values is null)
        {
            return result;
        }

        for (var index = 0; index < values.Values.Count; index++)
        {
            var display = (index + 1).ToString(CultureInfo.InvariantCulture);
            result.Add(new OptionModel { name = display, value = display });
        }

        return result;
    }

    public async Task<IReadOnlyList<OptionModel>> BuildHeaderOptionsAsync(string spreadsheetId, string sheetName)
    {
        var values = await GetSheetValuesAsync(spreadsheetId, sheetName, "1:1");
        var result = new List<OptionModel>();
        if (values?.Values is null || values.Values.Count == 0)
        {
            return result;
        }

        foreach (var header in values.Values[0].Where(h => !string.IsNullOrWhiteSpace(h)))
        {
            result.Add(new OptionModel { name = header, value = header });
        }

        return result;
    }

    public async Task<int?> GetSheetIdByTitleAsync(string spreadsheetId, string sheetTitle)
    {
        var spreadsheet = await GetSpreadsheetAsync(spreadsheetId);
        var sheet = spreadsheet?.Sheets?.FirstOrDefault(s => sheetTitle.Equals(s.Properties?.Title, StringComparison.OrdinalIgnoreCase));
        return sheet?.Properties?.SheetId;
    }

    public async Task<(int SheetId, string Title)> AddSheetAsync(string spreadsheetId, string sheetTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetTitle);

        var body = new
        {
            requests = new object[]
            {
                new
                {
                    addSheet = new
                    {
                        properties = new
                        {
                            title = sheetTitle
                        }
                    }
                }
            }
        };

        var response = await _credentials.Client.PostAsync($"v4/spreadsheets/{spreadsheetId}:batchUpdate", null, null, body);
        var payload = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create sheet '{sheetTitle}'. Status {(int)response.StatusCode} {response.StatusCode}. Content: {payload}");
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (root.TryGetProperty("replies", out var replies) && replies.ValueKind == JsonValueKind.Array && replies.GetArrayLength() > 0)
            {
                var first = replies[0];
                if (first.TryGetProperty("addSheet", out var addSheet) &&
                    addSheet.TryGetProperty("properties", out var props))
                {
                    var id = props.GetProperty("sheetId").GetInt32();
                    var title = props.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String
                        ? titleProp.GetString() ?? sheetTitle
                        : sheetTitle;
                    return (id, title);
                }
            }
        }
        catch (JsonException)
        {
            // Fall through to error below
        }

        throw new Exception("Sheets API did not return the created sheet details.");
    }

    public async Task DeleteSheetAsync(string spreadsheetId, string sheetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetId);

        var body = new
        {
            requests = new object[]
            {
                new
                {
                    deleteSheet = new
                    {
                        sheetId = sheetId
                    }
                }
            }
        };

        var response = await _credentials.Client.PostAsync($"v4/spreadsheets/{spreadsheetId}:batchUpdate", null, null, body);
        if (!response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to delete sheet '{sheetId}'. Status {(int)response.StatusCode} {response.StatusCode}. Content: {payload}");
        }
    }
}
