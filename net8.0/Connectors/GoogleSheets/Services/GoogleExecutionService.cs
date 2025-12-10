using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GoogleSheetsAction.Models;
using Ringhel.Procesio.Action.Core.Models;
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

    #region Design-Time Helper Methods

    /// <summary>
    /// Gets available Google Drives for the authenticated user.
    /// Used by event handlers to populate Drive dropdown options.
    /// </summary>
    public async Task<IReadOnlyList<GoogleDriveItem>> GetDrives()
    {
        var driveClient = new GoogleDriveClient(_drive);
        return await driveClient.ListDrivesAsync();
    }

    /// <summary>
    /// Gets spreadsheets from a specific drive.
    /// Used by event handlers to populate Spreadsheet dropdown options.
    /// </summary>
    public async Task<IReadOnlyList<GoogleDriveFile>> GetSpreadsheets(string? driveId)
    {
        if (string.IsNullOrWhiteSpace(driveId))
        {
            throw new Exception("Drive is required.");
        }

        var driveClient = new GoogleDriveClient(_drive);
        return await driveClient.ListSpreadsheetsAsync(driveId);
    }

    /// <summary>
    /// Gets sheets (tabs) within a spreadsheet.
    /// Used by event handlers to populate Sheet dropdown options.
    /// </summary>
    public async Task<IReadOnlyList<OptionModel>> GetSheets(string? spreadsheetId)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new Exception("Spreadsheet is required.");
        }

        var sheetsClient = new GoogleSheetsClient(_sheets);
        var spreadsheet = await sheetsClient.GetSpreadsheetAsync(spreadsheetId);

        if (spreadsheet?.Sheets is null)
        {
            return new List<OptionModel>();
        }

        var result = new List<OptionModel>();
        foreach (var sheet in spreadsheet.Sheets)
        {
            if (sheet.Properties is { } properties)
            {
                result.Add(new OptionModel
                {
                    name = string.IsNullOrWhiteSpace(properties.Title) 
                        ? properties.SheetId.ToString()
                        : properties.Title,
                    value = properties.SheetId.ToString()
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Gets header column options from a sheet.
    /// Used by event handlers to populate Key Column and other header-based dropdowns.
    /// </summary>
    public async Task<IReadOnlyList<OptionModel>> GetSheetHeaders(string? spreadsheetId, string? sheetId)
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
        var sheetTitle = await ResolveSheetTitleAsync(sheetsClient, spreadsheetId, sheetId);
        return await sheetsClient.BuildHeaderOptionsAsync(spreadsheetId, sheetTitle);
    }

    /// <summary>
    /// Gets row number options from a sheet.
    /// Used by event handlers to populate Row Number dropdown for UpdateRowByRange action.
    /// </summary>
    public async Task<IReadOnlyList<OptionModel>> GetRowNumbers(string? spreadsheetId, string? sheetId)
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
        var sheetTitle = await ResolveSheetTitleAsync(sheetsClient, spreadsheetId, sheetId);
        return await sheetsClient.BuildRowNumberOptionsAsync(spreadsheetId, sheetTitle);
    }

    #endregion

    #region Runtime Execution Methods

    public async Task<object?> CreateSpreadsheet(
        string? spreadsheetTitle,
        string? driveId,
        List<string>? headers)
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
        var headerValues = CleanHeaders(headers);
        if (headerValues.Count > 0)
        {
            await sheetsClient.UpdateHeadersAsync(defaultSheetTitle, spreadsheetId, headerValues);
        }

        return spreadsheetNode.ToJsonString(SerializerOptions);
    }

    /// <summary>
    /// Cleans and deduplicates a list of header strings.
    /// Trims whitespace and removes duplicate headers (case-insensitive).
    /// </summary>
    private static List<string> CleanHeaders(List<string>? headers)
    {
        return headers?
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h.Trim())
            .GroupBy(h => h, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList() ?? new();
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
        List<string>? headers)
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
                // Get spreadsheet to check sheet count and properties
                var existing = await sheetsClient.GetSpreadsheetAsync(spreadsheetId);
                var sheetsCount = existing?.Sheets?.Count ?? 0;

                if (sheetsCount <= 1)
                {
                    // Only one sheet exists - need to decide strategy based on sheet size
                    var existingSheet = existing?.Sheets?.FirstOrDefault(s => s.Properties?.SheetId == existingId.Value);
                    var rowCount = existingSheet?.Properties?.GridProperties?.RowCount ?? 0;

                    // Performance threshold: sheets larger than this will use create-rename-delete strategy
                    const int clearPerformanceThreshold = 10_000;

                    if (rowCount > clearPerformanceThreshold)
                    {
                        // STRATEGY 1: CREATE-RENAME-DELETE (Optimized for large sheets)
                        // This is faster for large sheets as it avoids clearing millions of cells
                        
                        // Step 1: Create temporary sheet
                        var tempName = $"{title}_temp_{Guid.NewGuid():N}";
                        var (newSheetId, _) = await sheetsClient.AddSheetAsync(spreadsheetId, tempName);

                        // Step 2: Apply headers to new sheet if provided
                        var headerValues = CleanHeaders(headers);
                        if (headerValues.Count > 0)
                        {
                            await sheetsClient.UpdateHeadersAsync(tempName, spreadsheetId, headerValues);
                        }

                        // Step 3: Batch rename new sheet and delete old sheet in a single API call
                        var batchRequests = new List<object>
                        {
                            new
                            {
                                updateSheetProperties = new
                                {
                                    properties = new
                                    {
                                        sheetId = newSheetId,
                                        title
                                    },
                                    fields = "title"
                                }
                            },
                            new
                            {
                                deleteSheet = new
                                {
                                    sheetId = existingId.Value
                                }
                            }
                        };

                        await sheetsClient.BatchUpdateAsync(spreadsheetId, batchRequests);

                        return new
                        {
                            spreadsheetId,
                            sheetId = newSheetId,
                            title,
                            strategy = "create-rename-delete",
                            message = $"Existing sheet was replaced with a new one (optimized for large sheets with {rowCount:N0} rows)"
                        };
                    }
                    else
                    {
                        // STRATEGY 2: CLEAR & REUSE (Preserves sheet ID for small sheets)
                        // This is better for small sheets as it maintains external references
                        
                        // Clear all data from the sheet
                        await sheetsClient.ClearRangeAsync(spreadsheetId, title);

                        // Apply headers if provided
                        var headerValues = CleanHeaders(headers);
                        if (headerValues.Count > 0)
                        {
                            await sheetsClient.UpdateHeadersAsync(title, spreadsheetId, headerValues);
                        }

                        return new
                        {
                            spreadsheetId,
                            sheetId = existingId.Value,
                            title,
                            strategy = "clear-and-reuse",
                            message = "Existing sheet was cleared and reused (preserves sheet ID for external references)"
                        };
                    }
                }
                else
                {
                    // Multiple sheets exist: safe to delete the old one
                    await sheetsClient.DeleteSheetAsync(spreadsheetId, existingId.Value.ToString());
                }
            }
        }

        // Create new sheet (either no existing sheet found, or it was deleted)
        var (createdSheetId, createdTitle) = await sheetsClient.AddSheetAsync(spreadsheetId, title);

        var newHeaderValues = CleanHeaders(headers);
        if (newHeaderValues.Count > 0)
        {
            await sheetsClient.UpdateHeadersAsync(createdTitle, spreadsheetId, newHeaderValues);
        }

        return new
        {
            spreadsheetId,
            sheetId = createdSheetId,
            title = createdTitle
        };
    }

    public async Task<object?> DeleteSheet(string? spreadsheetId, string? sheetId)
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

    public async Task<object?> ClearRange(
        string? spreadsheetId,
        string? sheetId,
        string? range)
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
        var sheetTitle = await ResolveSheetTitleAsync(sheetsClient, spreadsheetId, sheetId);

        var payload = await sheetsClient.ClearRangeAsync(spreadsheetId, sheetTitle, range);
        return JsonNode.Parse(payload)?.ToJsonString(SerializerOptions) ?? payload;
    }

    public async Task<object?> DeleteDimension(
        string? spreadsheetId,
        string? sheetId,
        string? dimension,
        int? startIndex,
        int? endIndex)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new Exception("Spreadsheet is required.");
        }
        if (string.IsNullOrWhiteSpace(sheetId))
        {
            throw new Exception("Sheet is required.");
        }
        if (string.IsNullOrWhiteSpace(dimension))
        {
            throw new Exception("Direction (dimension) is required.");
        }
        if (!startIndex.HasValue)
        {
            throw new Exception("Start index is required.");
        }

        var sheetsClient = new GoogleSheetsClient(_sheets);
        var payload = await sheetsClient.DeleteDimensionAsync(spreadsheetId, sheetId, dimension, startIndex, endIndex);
        return JsonNode.Parse(payload)?.ToJsonString(SerializerOptions) ?? payload;
    }

    public async Task<object?> GetRows(
        string? spreadsheetId,
        string? sheetId,
        string? range)
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
        var sheetTitle = await ResolveSheetTitleAsync(sheetsClient, spreadsheetId, sheetId);

        var sheetData = await sheetsClient.GetSheetValuesAsync(spreadsheetId, sheetTitle, range);
        return JsonSerializer.Serialize(sheetData, SerializerOptions);
    }

    public async Task<object?> UpdateRowByRange(
        string? spreadsheetId,
        string? sheetId,
        string? rowNumber,
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
        if (string.IsNullOrWhiteSpace(rowNumber))
        {
            throw new Exception("Row Number is required.");
        }
        if (string.IsNullOrWhiteSpace(rowValuesJson))
        {
            throw new Exception("Row Values are required.");
        }

        var sheetsClient = new GoogleSheetsClient(_sheets);
        var sheetTitle = await ResolveSheetTitleAsync(sheetsClient, spreadsheetId, sheetId);
        var inputMap = ParseRowValuesJson(rowValuesJson);
        var (headers, orderedValues) = await PrepareRowValuesAsync(sheetsClient, spreadsheetId, sheetTitle, inputMap);

        var payload = await sheetsClient.UpdateRowByRangeAsync(spreadsheetId, sheetTitle, rowNumber, orderedValues);
        return JsonNode.Parse(payload)?.ToJsonString(SerializerOptions) ?? payload;
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
        var sheetData = await sheetsClient.GetSheetValuesAsync(spreadsheetId, sheetTitle);

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
                // Case-sensitive comparison to ensure data integrity.
                // Example: "ID-001" and "id-001" should be treated as different values,
                // as many systems use case-sensitive identifiers (SKUs, order IDs, etc.)
                if (row.Count > 0 && 
                    string.Equals(row[0], keyValue, StringComparison.Ordinal))
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
                // Case-sensitive comparison to ensure data integrity.
                // Example: "ID-001" and "id-001" should be treated as different values,
                // as many systems use case-sensitive identifiers (SKUs, order IDs, etc.)
                if (keyColumnIndex < row.Count && 
                    string.Equals(row[keyColumnIndex], keyValue, StringComparison.Ordinal))
                {
                    return i + 1; // Return 1-based row number
                }
            }
        }

        return null;
    }

    #endregion
}
