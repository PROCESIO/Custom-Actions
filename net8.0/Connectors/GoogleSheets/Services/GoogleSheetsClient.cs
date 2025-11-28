using GoogleSheetsAction.Models;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;
using System.Globalization;
using System.Text.Json;

namespace GoogleSheetsAction.Services;

public sealed class GoogleSheetsClient
{
    #region constants
    /// <summary>
    /// API version for Google Sheets API.
    /// </summary>
    public const string ApiVersion = "v4";

    /// <summary>
    /// Default column range for reading/writing sheet data.
    /// A:ZZ covers 702 columns, which is sufficient for most use cases.
    /// </summary>
    public const string DefaultColumnRange = "A:ZZ";

    /// <summary>
    /// Range for reading header row (first row).
    /// </summary>
    public const string HeaderRowRange = "1:1";

    /// <summary>
    /// Represents the query parameter name used to specify the value input option in a request.
    /// </summary>
    public const string ValueInputOptionQuery = "valueInputOption";

    /// <summary>
    /// Represents the query parameter name used to specify the insert data option in a request.
    /// </summary>
    public const string InsertDataOptionQuery = "insertDataOption";

    /// <summary>
    /// Value input option that allows Google Sheets to parse data intelligently.
    /// USER_ENTERED means numbers, dates, and formulas are interpreted as if typed by a user.
    /// </summary>
    public const string UserEnteredValueInputOption = "USER_ENTERED";

    /// <summary>
    /// Insert data option for appending rows.
    /// </summary>
    public const string InsertRowsOption = "INSERT_ROWS";
    #endregion

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

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}";
        var response = await _credentials.Client.GetAsync(endpoint, null, null);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GoogleSpreadsheetResponse>(payload);
    }

    public async Task<string> CreateSpreadSheetAsync(string? spreadSheetTitle)
    {
        var title = spreadSheetTitle?.Trim();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

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
            var endpoint = $"{ApiVersion}/spreadsheets";
            createResponse = await _credentials.Client.PostAsync(endpoint, null, null, request);
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
        string? spreadsheetId,
        IList<string>? headerValues)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentNullException.ThrowIfNull(headerValues);

        var targetSheet = string.IsNullOrWhiteSpace(defaultSheetTitle) ? "Sheet1" : defaultSheetTitle;
        var range = $"{targetSheet}!{HeaderRowRange}";
        var updateQuery = new Dictionary<string, string>
        {
            [ValueInputOptionQuery] = UserEnteredValueInputOption
        };

        var updateBody = new
        {
            values = new List<IList<string>> { headerValues }
        };

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(range)}";
        var updateResponse = await _credentials.Client.PutAsync(endpoint, updateQuery, null, updateBody);

        if (!updateResponse.IsSuccessStatusCode)
        {
            var updatePayload = await updateResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to apply headers to the new spreadsheet. Status {(int)updateResponse.StatusCode} {updateResponse.StatusCode}. Content: {updatePayload}");
        }
    }

    public async Task<GoogleSheetValueRange?> GetSheetValuesAsync(string? spreadsheetId, string? sheetName, string? range = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);

        var effectiveRange = range ?? DefaultColumnRange;
        var relativeRange = string.IsNullOrEmpty(effectiveRange) ? sheetName : $"{sheetName}!{effectiveRange}";
        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(relativeRange)}";
        var response = await _credentials.Client.GetAsync(endpoint, null, null);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GoogleSheetValueRange>(payload);
    }

    public async Task<IReadOnlyList<OptionModel>> BuildRowNumberOptionsAsync(string spreadsheetId, string sheetName)
    {
        var values = await GetSheetValuesAsync(spreadsheetId, sheetName, DefaultColumnRange);
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

    public async Task<IReadOnlyList<OptionModel>> BuildHeaderOptionsAsync(
        string spreadsheetId,
        string sheetName)
    {
        var values = await GetSheetValuesAsync(spreadsheetId, sheetName, HeaderRowRange);
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

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}:batchUpdate";
        var response = await _credentials.Client.PostAsync(endpoint, null, null, body);
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

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}:batchUpdate";
        var response = await _credentials.Client.PostAsync(endpoint, null, null, body);
        if (!response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to delete sheet '{sheetId}'. Status {(int)response.StatusCode} {response.StatusCode}. Content: {payload}");
        }
    }

    public async Task<string> AppendRowAsync(
        string? spreadsheetId,
        string? sheetName,
        IList<string>? values,
        string? valueInputOption = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);

        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        // Use a wide column range to ensure all values fit; the API will append to the next available row
        var range = $"{sheetName}!{DefaultColumnRange}";
        var query = new Dictionary<string, string>
        {
            [ValueInputOptionQuery] = valueInputOption ?? UserEnteredValueInputOption,
            [InsertDataOptionQuery] = InsertRowsOption
        };

        var body = new
        {
            values = new List<IList<string>> { values }
        };

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(range)}:append";
        var response = await _credentials.Client.PostAsync(endpoint, query, null, body);
        var payload = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to append row. Status {(int)response.StatusCode} {response.StatusCode}. Content: {payload}");
        }

        return payload;
    }

    public async Task<string> UpdateRowAsync(
        string? spreadsheetId,
        string? sheetName,
        int rowNumber,
        IList<string>? values,
        string? valueInputOption = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }
        if (rowNumber < 1)
        {
            throw new ArgumentException("Row number must be greater than 0.", nameof(rowNumber));
        }

        // Build range for the specific row (e.g., "Sheet1!A2:ZZ2" for row 2)
        var range = $"{sheetName}!A{rowNumber}:ZZ{rowNumber}";
        var query = new Dictionary<string, string>
        {
            [ValueInputOptionQuery] = valueInputOption ?? UserEnteredValueInputOption
        };

        var body = new
        {
            values = new List<IList<string>> { values }
        };

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(range)}";
        var response = await _credentials.Client.PutAsync(endpoint, query, null, body);
        var payload = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to update row {rowNumber}. Status {(int)response.StatusCode} {response.StatusCode}. Content: {payload}");
        }

        return payload;
    }

    public async Task<string> ClearRangeAsync(
        string? spreadsheetId,
        string? sheetName,
        string? range = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);

        // If no range is provided, clear the entire sheet using a wide range
        var effectiveRange = range ?? DefaultColumnRange;
        var relativeRange = $"{sheetName}!{effectiveRange}";

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(relativeRange)}:clear";
        var response = await _credentials.Client.PostAsync(endpoint, null, null, new { });

        var payload = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to clear range '{relativeRange}'. Status {(int)response.StatusCode} {response.StatusCode}. Content: {payload}");
        }

        return payload;
    }

    public async Task<string> DeleteDimensionAsync(
        string? spreadsheetId,
        string? sheetId,
        string? dimension,
        int? startIndex,
        int? endIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dimension);
        
        if (!int.TryParse(sheetId, out var sheetIdInt))
        {
            throw new ArgumentException("SheetId must be a valid integer.", nameof(sheetId));
        }

        if (!startIndex.HasValue)
        {
            throw new ArgumentException("Start index is required.", nameof(startIndex));
        }

        // Validate dimension is either ROWS or COLUMNS
        if (!dimension.Equals("ROWS", StringComparison.OrdinalIgnoreCase) &&
            !dimension.Equals("COLUMNS", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Dimension must be either 'ROWS' or 'COLUMNS'.", nameof(dimension));
        }

        var body = new
        {
            requests = new object[]
            {
                new
                {
                    deleteDimension = new
                    {
                        range = new
                        {
                            sheetId = sheetIdInt,
                            dimension = dimension.ToUpperInvariant(),
                            startIndex = startIndex.Value,
                            endIndex = endIndex
                        }
                    }
                }
            }
        };

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}:batchUpdate";
        var response = await _credentials.Client.PostAsync(endpoint, null, null, body);

        var payload = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to delete dimension. Status {(int)response.StatusCode} {response.StatusCode}. Content: {payload}");
        }

        return payload;
    }

    public async Task<string> GetRowsAsync(
        string? spreadsheetId,
        string? sheetName,
        string? range = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);

        var effectiveRange = range ?? DefaultColumnRange;
        var relativeRange = $"{sheetName}!{effectiveRange}";

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(relativeRange)}";
        var response = await _credentials.Client.GetAsync(endpoint, null, null);

        var payload = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get rows from range '{relativeRange}'. Status {(int)response.StatusCode} {response.StatusCode}. Content: {payload}");
        }

        return payload;
    }

    public async Task<string> UpdateRowByRangeAsync(
        string? spreadsheetId,
        string? sheetName,
        string? rowNumber,
        IList<string>? values,
        string? valueInputOption = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(rowNumber);
        
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (!int.TryParse(rowNumber, out var rowNum) || rowNum < 1)
        {
            throw new ArgumentException("Row number must be a positive integer.", nameof(rowNumber));
        }

        // Build range for the specific row (e.g., "Sheet1!A2:ZZ2" for row 2)
        var range = $"{sheetName}!A{rowNum}:ZZ{rowNum}";
        var query = new Dictionary<string, string>
        {
            [ValueInputOptionQuery] = valueInputOption ?? UserEnteredValueInputOption
        };

        var body = new
        {
            values = new List<IList<string>> { values }
        };

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(range)}";
        var response = await _credentials.Client.PutAsync(endpoint, query, null, body);

        var payload = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to update row {rowNum}. Status {(int)response.StatusCode} {response.StatusCode}. Content: {payload}");
        }

        return payload;
    }

    public async Task RenameSheetAsync(string spreadsheetId, int sheetId, string newTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newTitle);

        var body = new
        {
            requests = new object[]
            {
                new
                {
                    updateSheetProperties = new
                    {
                        properties = new
                        {
                            sheetId = sheetId,
                            title = newTitle
                        },
                        fields = "title"
                    }
                }
            }
        };

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}:batchUpdate";
        var response = await _credentials.Client.PostAsync(endpoint, null, null, body);

        if (!response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to rename sheet '{sheetId}' to '{newTitle}'. Status {(int)response.StatusCode} {response.StatusCode}. Content: {payload}");
        }
    }

    /// <summary>
    /// Executes multiple requests in a single batch operation for better performance.
    /// Use this to combine operations like rename + delete to reduce API calls.
    /// </summary>
    /// <param name="spreadsheetId">The spreadsheet ID</param>
    /// <param name="requests">Array of request objects to execute</param>
    /// <returns>The raw JSON response from the API</returns>
    public async Task<string> BatchUpdateAsync(string spreadsheetId, IEnumerable<object>? requests)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);

        var requestsList = requests?.ToList() ?? throw new ArgumentNullException(nameof(requests));
        if (requestsList.Count == 0)
        {
            throw new ArgumentException("At least one request must be provided.", nameof(requests));
        }

        var body = new { requests = requestsList };

        var endpoint = $"{ApiVersion}/spreadsheets/{spreadsheetId}:batchUpdate";
        var response = await _credentials.Client.PostAsync(endpoint, null, null, body);

        var payload = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Batch update failed. Status {(int)response.StatusCode} {response.StatusCode}. Content: {payload}");
        }

        return payload;
    }
}
