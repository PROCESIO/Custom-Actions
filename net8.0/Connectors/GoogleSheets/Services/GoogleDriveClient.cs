using System.Globalization;
using System.Text.Json;
using GoogleSheetsAction.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;

namespace GoogleSheetsAction.Services;

public sealed class GoogleDriveClient
{
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

            var response = await _credentials.Client!.GetAsync("drive/v3/drives", query, new());
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
        string driveId,
        string spreadSheetId)
    {
        var driveQuery = new Dictionary<string, string>
        {
            ["supportsAllDrives"] = "true",
            ["includeItemsFromAllDrives"] = "true",
            ["addParents"] = driveId,
            ["removeParents"] = "root"
        };

        var patchResponse = await _credentials.Client.PatchAsync($"drive/v3/files/{spreadSheetId}", driveQuery, null, null);
        if (!patchResponse.IsSuccessStatusCode)
        {
            var patchPayload = await patchResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to assign the spreadsheet to drive '{driveId}'. Status {(int)patchResponse.StatusCode} {patchResponse.StatusCode}. Content: {patchPayload}");
        }
    }

    public async Task DeleteFileAsync(string fileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        var query = new Dictionary<string, string>
        {
            ["supportsAllDrives"] = "true",
            ["includeItemsFromAllDrives"] = "true"
        };

        var deleteResponse = await _credentials.Client.DeleteAsync($"drive/v3/files/{fileId}", query, null);
        if (!deleteResponse.IsSuccessStatusCode)
        {
            var payload = await deleteResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to delete file '{fileId}'. Status {(int)deleteResponse.StatusCode} {deleteResponse.StatusCode}. Content: {payload}");
        }
    }

    public async Task<IReadOnlyList<GoogleDriveFile>> ListSpreadsheetsAsync(string driveId, int pageSize = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveId);

        var result = new List<GoogleDriveFile>();
        var query = new Dictionary<string, string>
        {
            ["q"] = "mimeType='application/vnd.google-apps.spreadsheet'",
            ["corpora"] = "drive",
            ["driveId"] = driveId,
            ["includeItemsFromAllDrives"] = "true",
            ["supportsAllDrives"] = "true",
            ["fields"] = "nextPageToken,files(id,name,webViewLink)",
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
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

            var response = await _credentials.Client!.GetAsync("drive/v3/files", query, new());
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
}
