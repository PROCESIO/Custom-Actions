using Ringhel.Procesio.Action.Core.Models.Credentials.API;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

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
        var createPayloadResponse = await sheetsClient.CreateSpreadSheetAsync(spreadsheetTitle!);

        //Parse the response after creating the new spreadsheet
        var spreadsheetNode = JsonNode.Parse(createPayloadResponse, new JsonNodeOptions { PropertyNameCaseInsensitive = false });
        if (spreadsheetNode is null)
        {
            throw new Exception($"Unable to parse the Sheets API response");
        }

        //Validate that the new spreadsheet has id
        var spreadsheetId = spreadsheetNode["spreadsheetId"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new Exception("The Sheets API response did not include a spreadsheetId.");
        }

        //Get the default sheet title
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

        //Move the file to the right drive
        var driveClient = new GoogleDriveClient(_drive);
        if (!string.IsNullOrWhiteSpace(driveId) &&
            !string.Equals(driveId, "root", StringComparison.OrdinalIgnoreCase) &&
            _drive?.Client is not null)
        {
            await driveClient.UpdateFileLocationAsync(driveId, spreadsheetId);
        }

        //Update the spreadsheet headers
        var headerValues = ParseHeaders(headers);
        if (headerValues.Count > 0)
        {
            await sheetsClient.UpdateHeaders(defaultSheetTitle, spreadsheetId, headerValues);
        }

        return spreadsheetNode;
    }

    public async Task<object?> DeleteSpreadsheet(string? spreadsheetId)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new Exception("Spreadsheet is required.");
        }

        var driveClient = new GoogleDriveClient(_drive);
        try
        {
            await driveClient.DeleteFileAsync(spreadsheetId);
        }
        catch (Exception)
        {
            return false;
        }

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
                    await sheetsClient.DeleteSheetAsync(spreadsheetId, existingId.Value);
                }
            }
        }

        var (createdSheetId, createdTitle) = await sheetsClient.AddSheetAsync(spreadsheetId, title);

        var headerValues = ParseHeaders(headers);
        if (headerValues.Count > 0)
        {
            await sheetsClient.UpdateHeaders(createdTitle, spreadsheetId, headerValues);
        }

        return new
        {
            spreadsheetId,
            sheetId = createdSheetId,
            title = createdTitle
        };
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
}
