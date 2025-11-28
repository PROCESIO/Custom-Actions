using System.Globalization;
using System.Text.Json;
using GoogleSheetsAction.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;

namespace GoogleSheetsAction.Services;

public sealed class GoogleDriveClient
{
    #region constants
    /// <summary>
    /// API version for Google Drive API.
    /// </summary>
    public const string ApiVersion = "v3";

    /// <summary>
    /// Special drive ID representing the user's personal "My Drive".
    /// </summary>
    public const string RootDriveId = "root";

    /// <summary>
    /// Represents the query parameter to indicate support for all drives.
    /// </summary>
    public const string SupportsAllDrivesQuery = "supportsAllDrives";

    /// <summary>
    /// Represents the query parameter to include items from all drives.
    /// </summary>
    public const string IncludeItemsFromAllDrivesQuery = "includeItemsFromAllDrives";
    #endregion

    private readonly APICredentialsManager _credentials;

    public GoogleDriveClient(APICredentialsManager? credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        if (_credentials.Client is null)
        {
            throw new ArgumentException("Credentials client is not configured.", nameof(credentials));
        }
    }

    public async Task<IReadOnlyList<GoogleDriveItem>> ListDrivesAsync(int pageSize = 100)
    {
        var result = new List<GoogleDriveItem>();
        var query = new Dictionary<string, string>
        {
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
            ["fields"] = "nextPageToken,drives(id,name)"
        };

        string? pageToken = null;
        do
        {
            if (!string.IsNullOrEmpty(pageToken))
            {
                query["pageToken"] = pageToken;
            }
            else
            {
                query.Remove("pageToken");
            }

            var endpoint = $"drive/{ApiVersion}/drives";
            var response = await _credentials.Client.GetAsync(endpoint, query, null);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync();
            var drives = JsonSerializer.Deserialize<GoogleDriveListResponse>(payload);
            if (drives?.Drives is { Count: > 0 })
            {
                result.AddRange(drives.Drives.Where(d => !string.IsNullOrWhiteSpace(d.Id)));
            }

            pageToken = drives?.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return result;
    }

    public async Task UpdateFileLocationAsync(
        string? driveId,
        string? spreadSheetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveId);
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadSheetId);

        var driveQuery = new Dictionary<string, string>
        {
            [SupportsAllDrivesQuery] = "true",
            [IncludeItemsFromAllDrivesQuery] = "true",
            ["addParents"] = driveId,
            ["removeParents"] = RootDriveId
        };

        var endpoint = $"drive/{ApiVersion}/files/{spreadSheetId}";
        var patchResponse = await _credentials.Client.PatchAsync(endpoint, driveQuery, null, null);
        if (!patchResponse.IsSuccessStatusCode)
        {
            var patchPayload = await patchResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to assign the spreadsheet to drive '{driveId}'. Status {(int)patchResponse.StatusCode} {patchResponse.StatusCode}. Content: {patchPayload}");
        }
    }

    public async Task<IReadOnlyList<GoogleDriveFile>> ListSpreadsheetsAsync(string? driveId, int pageSize = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveId);

        var result = new List<GoogleDriveFile>();
        var query = new Dictionary<string, string>
        {
            ["q"] = "mimeType='application/vnd.google-apps.spreadsheet'",
            [IncludeItemsFromAllDrivesQuery] = "true",
            [SupportsAllDrivesQuery] = "true",
            ["fields"] = "nextPageToken,files(id,name,webViewLink)",
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        };

        if (driveId.Equals(RootDriveId, StringComparison.OrdinalIgnoreCase))
        {
            query["corpora"] = "user";
        }
        else
        {
            query["corpora"] = "drive";
            query["driveId"] = driveId;
        }

        string? pageToken = null;
        do
        {
            if (!string.IsNullOrEmpty(pageToken))
            {
                query["pageToken"] = pageToken;
            }
            else
            {
                query.Remove("pageToken");
            }

            var endpoint = $"drive/{ApiVersion}/files";
            var response = await _credentials.Client.GetAsync(endpoint, query, null);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<GoogleDriveFileListResponse>(payload);
            if (files?.Files is { Count: > 0 })
            {
                result.AddRange(files.Files.Where(f => !string.IsNullOrWhiteSpace(f.Id)));
            }

            pageToken = files?.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return result;
    }

    public async Task DeleteFileAsync(string spreadsheetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);

        var query = new Dictionary<string, string>
        {
            [SupportsAllDrivesQuery] = "true",
            [IncludeItemsFromAllDrivesQuery] = "true"
        };

        var endpoint = $"drive/{ApiVersion}/files/{spreadsheetId}";
        var deleteResponse = await _credentials.Client.DeleteAsync(endpoint, query, null);
        if (!deleteResponse.IsSuccessStatusCode)
        {
            var payload = await deleteResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to delete file '{spreadsheetId}'. Status {(int)deleteResponse.StatusCode} {deleteResponse.StatusCode}. Content: {payload}");
        }
    }
}
