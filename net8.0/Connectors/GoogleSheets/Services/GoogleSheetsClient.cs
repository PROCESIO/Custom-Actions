using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GoogleSheetsAction.Models;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;

namespace GoogleSheetsAction.Services
{
    public class GoogleSheetsClient
    {
        private const string BaseUrl = "https://sheets.googleapis.com/v4";
        private readonly APICredentialsManager _credentials;

        public GoogleSheetsClient(APICredentialsManager credentials)
        {
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            if (_credentials.Client == null)
            {
                throw new ArgumentException("Credentials client is not configured.", nameof(credentials));
            }
        }

        public async Task<GoogleSpreadsheetResponse?> GetSpreadsheetAsync(string spreadsheetId)
        {
            if (string.IsNullOrWhiteSpace(spreadsheetId))
            {
                throw new ArgumentException("Spreadsheet id cannot be empty.", nameof(spreadsheetId));
            }

            var response = await _credentials.Client!.GetAsync($"{BaseUrl}/spreadsheets/{spreadsheetId}", new(), new()).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<GoogleSpreadsheetResponse>(payload);
        }

        public async Task<GoogleSheetValueRange?> GetSheetValuesAsync(string spreadsheetId, string sheetName, string range = "A:Z")
        {
            if (string.IsNullOrWhiteSpace(spreadsheetId))
            {
                throw new ArgumentException("Spreadsheet id cannot be empty.", nameof(spreadsheetId));
            }

            if (string.IsNullOrWhiteSpace(sheetName))
            {
                throw new ArgumentException("Sheet name cannot be empty.", nameof(sheetName));
            }

            var relativeRange = string.IsNullOrEmpty(range) ? sheetName : $"{sheetName}!{range}";
            var response = await _credentials.Client!.GetAsync($"{BaseUrl}/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(relativeRange)}", new(), new()).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<GoogleSheetValueRange>(payload);
        }

        public async Task<IReadOnlyList<OptionModel>> BuildRowNumberOptionsAsync(string spreadsheetId, string sheetName)
        {
            var values = await GetSheetValuesAsync(spreadsheetId, sheetName, "A:Z").ConfigureAwait(false);
            var result = new List<OptionModel>();
            if (values?.Values == null)
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
            var values = await GetSheetValuesAsync(spreadsheetId, sheetName, "1:1").ConfigureAwait(false);
            var result = new List<OptionModel>();
            if (values?.Values == null || values.Values.Count == 0)
            {
                return result;
            }

            foreach (var header in values.Values[0].Where(h => !string.IsNullOrWhiteSpace(h)))
            {
                result.Add(new OptionModel { name = header, value = header });
            }

            return result;
        }
    }
}
