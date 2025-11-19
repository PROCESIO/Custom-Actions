using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;

namespace GoogleSheetsAction.Services;
internal class GoogleExecutionService
{
    private readonly APICredentialsManager? _sheets;
    private readonly APICredentialsManager? _drive;

    private static JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GoogleExecutionService(APICredentialsManager? sheets, APICredentialsManager? drive)
    {
        _sheets = sheets;
        _drive = drive;
    }

    public async Task<object?> CreateSpreadsheet(
        string? spreadsheetTitle,
        string? driveId,
        string? headers)
    {
        var sheetsClient = new GoogleSheetsClient(_sheets);
        var createPayloadResponse = await sheetsClient.CreateSpreadSheetAsync(spreadsheetTitle);

        // Parse the response after creating the new spreadsheet
        var spreadsheetNode = JsonNode.Parse(createPayloadResponse, new JsonNodeOptions { PropertyNameCaseInsensitive = false });
        if (spreadsheetNode is null)
        {
            throw new Exception("Unable to parse the Sheets API response");
        }

        // Validate that the new spreadsheet has id
        var spreadsheetId = spreadsheetNode["spreadsheetId"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new Exception("The Sheets API response did not include a spreadsheetId.");
        }

        // Get the default sheet title
        string? defaultSheetTitle = null;
        if (spreadsheetNode["sheets"] is JsonArray sheetsArray)
        {
            var firstSheet = sheetsArray.FirstOrDefault();
            if (firstSheet is JsonObject sheetObject &&
                sheetObject["properties"] is JsonObject properties &&
                properties["title"] is JsonNode sheetTitleNode)
            {
                defaultSheetTitle = sheetTitleNode.GetValue<string?>();
            }
        }

        // Move the file to the right drive
        var driveClient = new GoogleDriveClient(_drive);
        if (!string.IsNullOrWhiteSpace(driveId) &&
            !string.Equals(driveId, "root", StringComparison.OrdinalIgnoreCase) &&
            _drive?.Client is not null)
        {
            await driveClient.UpdateFileLocationAsync(driveId, spreadsheetId);
        }

        // Update the spreadsheet headers
        var headerValues = ParseHeaders(headers);
        if (headerValues.Count > 0)
        {
            await sheetsClient.UpdateHeadersAsync(defaultSheetTitle, spreadsheetId, headerValues);
        }

        return spreadsheetNode.ToJsonString(SerializerOptions);
    }

    private List<string> ParseHeaders(string? headers)
    {
        if (string.IsNullOrWhiteSpace(headers))
        {
            return new List<string>();
        }

        var trimmed = headers.Trim();
        try
        {
            if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
            {
                var asJson = JsonSerializer.Deserialize<List<string>>(trimmed, SerializerOptions);
                if (asJson is { Count: > 0 })
                {
                    return asJson
                        .Select(header => header.Trim())
                        .Where(header => !string.IsNullOrWhiteSpace(header))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(header => header!)
                        .ToList();
                }
            }
        }
        catch (JsonException)
        {
            // Ignore JSON parsing failures and fall back to delimiter-based parsing.
        }

        var separators = new[] { ',', ';', '\n', '\r', '\t' };
        return headers
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(header => header.Trim())
            .Where(header => !string.IsNullOrWhiteSpace(header))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<object?> DeleteSpreadsheet(string? spreadsheetId)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new Exception("Spreadsheet is required.");
        }

        var driveClient = new GoogleDriveClient(_drive);
        await driveClient.DeleteFileAsync(spreadsheetId);
        return true;
    }

    public async Task<object?> CreateSheet(
        string? spreadsheetId,
        string? newSheetTitle,
        bool overwrite,
        string? headers)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new Exception("Spreadsheet is required.");
        }

        var title = newSheetTitle?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new Exception("Sheet name is required.");
        }

        var sheetsClient = new GoogleSheetsClient(_sheets);

        if (overwrite)
        {
            var existingId = await sheetsClient.GetSheetIdByTitleAsync(spreadsheetId, title);
            if (existingId.HasValue)
            {
                // Avoid deleting the only sheet in a spreadsheet; Sheets API requires at least one sheet
                var existing = await sheetsClient.GetSpreadsheetAsync(spreadsheetId);
                var sheetsCount = existing?.Sheets?.Count ?? 0;
                if (sheetsCount <= 1)
                {
                    // If only one sheet exists and overwrite is requested, we will just rename it by creating a new sheet and deleting the old after
                    // but API disallows deleting last sheet; so skip delete here
                }
                else
                {
                    await sheetsClient.DeleteSheetAsync(spreadsheetId, existingId.Value.ToString());
                }
            }
        }

        var (createdSheetId, createdTitle) = await sheetsClient.AddSheetAsync(spreadsheetId, title);

        var headerValues = ParseHeaders(headers);
        if (headerValues.Count > 0)
        {
            await sheetsClient.UpdateHeadersAsync(createdTitle, spreadsheetId, headerValues);
        }

        return new
        {
            spreadsheetId,
            sheetId = createdSheetId,
            title = createdTitle
        };
    }

    public async Task<object?> DeleteSpread(string? spreadsheetId, string? sheetId)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new Exception("Spreadsheet is required.");
        }

        if (string.IsNullOrWhiteSpace(sheetId))
        {
            throw new Exception("Sheet is required.");
        }

        var sheetsClient = new GoogleSheetsClient(_sheets);
        await sheetsClient.DeleteSheetAsync(spreadsheetId, sheetId);
        return true;
    }

    public async Task<object?> AppendRow(
        string? spreadsheetId,
        string? sheetId,
        string? rowValuesJson)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new Exception("Spreadsheet is required.");
        }
        if (string.IsNullOrWhiteSpace(sheetId))
        {
            throw new Exception("Sheet is required.");
        }
        if (string.IsNullOrWhiteSpace(rowValuesJson))
        {
            throw new Exception("Row Values are required.");
        }

        var sheetsClient = new GoogleSheetsClient(_sheets);
        var sheetTitle = await ResolveSheetTitleAsync(sheetsClient, spreadsheetId, sheetId);
        var inputMap = ParseRowValuesJson(rowValuesJson);
        var (headers, orderedValues) = await PrepareRowValuesAsync(sheetsClient, spreadsheetId, sheetTitle, inputMap);

        var payload = await sheetsClient.AppendRowAsync(spreadsheetId, sheetTitle, orderedValues);
        return JsonNode.Parse(payload)?.ToJsonString(SerializerOptions) ?? payload;
    }

    public async Task<object?> AppendOrUpdateRow(
        string? spreadsheetId,
        string? sheetId,
        string? keyColumn,
        string? rowValuesJson)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new Exception("Spreadsheet is required.");
        }
        if (string.IsNullOrWhiteSpace(sheetId))
        {
            throw new Exception("Sheet is required.");
        }
        if (string.IsNullOrWhiteSpace(keyColumn))
        {
            throw new Exception("Key Column is required.");
        }
        if (string.IsNullOrWhiteSpace(rowValuesJson))
        {
            throw new Exception("Row Values are required.");
        }

        var sheetsClient = new GoogleSheetsClient(_sheets);
        var sheetTitle = await ResolveSheetTitleAsync(sheetsClient, spreadsheetId, sheetId);
        var inputMap = ParseRowValuesJson(rowValuesJson);
        var (headers, orderedValues) = await PrepareRowValuesAsync(sheetsClient, spreadsheetId, sheetTitle, inputMap);

        // Validate key column and get key value
        if (!inputMap.TryGetValue(keyColumn, out var keyValue))
        {
            throw new Exception($"Row Values must include a value for the key column '{keyColumn}'.");
        }

        if (string.IsNullOrWhiteSpace(keyValue))
        {
            throw new Exception($"Key column '{keyColumn}' value cannot be empty.");
        }

        // Find matching row
        var matchingRowNumber = await FindMatchingRowAsync(sheetsClient, spreadsheetId, sheetTitle, headers, keyColumn, keyValue);

        if (matchingRowNumber.HasValue)
        {
            // Update existing row
            var payload = await sheetsClient.UpdateRowAsync(spreadsheetId, sheetTitle, matchingRowNumber.Value, orderedValues);
            return JsonNode.Parse(payload)?.ToJsonString(SerializerOptions) ?? payload;
        }
        else
        {
            // Append new row
            var payload = await sheetsClient.AppendRowAsync(spreadsheetId, sheetTitle, orderedValues);
            return JsonNode.Parse(payload)?.ToJsonString(SerializerOptions) ?? payload;
        }
    }

    private async Task<string> ResolveSheetTitleAsync(GoogleSheetsClient sheetsClient, string spreadsheetId, string sheetId)
    {
        var spreadsheet = await sheetsClient.GetSpreadsheetAsync(spreadsheetId);
        var sheetTitle = spreadsheet?.Sheets?
            .FirstOrDefault(s => s.Properties != null && s.Properties.SheetId.ToString() == sheetId)?
            .Properties?.Title;
        
        if (string.IsNullOrWhiteSpace(sheetTitle))
        {
            throw new Exception($"Could not resolve sheet title for id '{sheetId}'.");
        }

        return sheetTitle;
    }

    private Dictionary<string, string?> ParseRowValuesJson(string rowValuesJson)
    {
        Dictionary<string, string?>? inputMap;
        try
        {
            inputMap = JsonSerializer.Deserialize<Dictionary<string, string?>>(rowValuesJson, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new Exception($"Invalid Row Values JSON. {ex.Message}");
        }

        if (inputMap is null || inputMap.Count == 0)
        {
            throw new Exception("Row Values JSON must contain at least one key-value pair.");
        }

        return inputMap;
    }

    private async Task<(List<string> Headers, List<string> OrderedValues)> PrepareRowValuesAsync(
        GoogleSheetsClient sheetsClient,
        string spreadsheetId,
        string sheetTitle,
        Dictionary<string, string?> inputMap)
    {
        var headerOptions = await sheetsClient.BuildHeaderOptionsAsync(spreadsheetId, sheetTitle);
        var headers = headerOptions.Select(o => o.name).ToList();

        if (headers.Count == 0)
        {
            // If there are no headers, use values in the order provided
            var valuesNoHeaders = inputMap.Values.Select(v => v ?? string.Empty).ToList();
            return (headers, valuesNoHeaders);
        }

        // Order values based on headers
        var orderedValues = headers
            .Select(h => inputMap.TryGetValue(h, out var v)
                ? v ?? string.Empty
                : string.Empty)
            .ToList();

        return (headers, orderedValues);
    }

    private async Task<int?> FindMatchingRowAsync(
        GoogleSheetsClient sheetsClient,
        string spreadsheetId,
        string sheetTitle,
        List<string> headers,
        string keyColumn,
        string keyValue)
    {
        var sheetData = await sheetsClient.GetSheetValuesAsync(spreadsheetId, sheetTitle, "A:Z");
        
        if (sheetData?.Values == null || sheetData.Values.Count == 0)
        {
            return null;
        }

        if (headers.Count == 0)
        {
            // No headers: use first column as key
            for (int i = 0; i < sheetData.Values.Count; i++)
            {
                var row = sheetData.Values[i];
                if (row.Count > 0 && 
                    string.Equals(row[0], keyValue, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1; // Return 1-based row number
                }
            }
        }
        else
        {
            // With headers: find key column index
            var keyColumnIndex = headers.IndexOf(keyColumn);
            if (keyColumnIndex < 0)
            {
                throw new Exception($"Key column '{keyColumn}' not found in sheet headers.");
            }

            // Skip header row (index 0), check data rows
            for (int i = 1; i < sheetData.Values.Count; i++)
            {
                var row = sheetData.Values[i];
                if (keyColumnIndex < row.Count && 
                    string.Equals(row[keyColumnIndex], keyValue, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1; // Return 1-based row number
                }
            }
        }

        return null;
    }
}
