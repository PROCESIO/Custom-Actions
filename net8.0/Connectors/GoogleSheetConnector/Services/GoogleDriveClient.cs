using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GoogleSheetConnector.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;

namespace GoogleSheetConnector.Services
{
    public class GoogleDriveClient
    {
        private const string BaseUrl = "https://www.googleapis.com/drive/v3";
        private readonly APICredentialsManager _credentials;

        public GoogleDriveClient(APICredentialsManager credentials)
        {
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            if (_credentials.Client == null)
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

                var response = await _credentials.Client!.GetAsync($"{BaseUrl}/drives", query, new()).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var drives = JsonSerializer.Deserialize<GoogleDriveListResponse>(payload);
                if (drives?.Drives != null && drives.Drives.Count > 0)
                {
                    result.AddRange(drives.Drives.Where(d => !string.IsNullOrWhiteSpace(d.Id)));
                }

                pageToken = drives?.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            return result;
        }

        public async Task<IReadOnlyList<GoogleDriveFile>> ListSpreadsheetsAsync(string driveId, int pageSize = 100)
        {
            if (string.IsNullOrWhiteSpace(driveId))
            {
                throw new ArgumentException("Drive id cannot be empty.", nameof(driveId));
            }

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

                var response = await _credentials.Client!.GetAsync($"{BaseUrl}/files", query, new()).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var files = JsonSerializer.Deserialize<GoogleDriveFileListResponse>(payload);
                if (files?.Files != null && files.Files.Count > 0)
                {
                    result.AddRange(files.Files.Where(f => !string.IsNullOrWhiteSpace(f.Id)));
                }

                pageToken = files?.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            return result;
        }
    }
}
