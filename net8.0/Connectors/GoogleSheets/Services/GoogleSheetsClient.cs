using System.Globalization;
using System.Text.Json;
using GoogleSheetsAction.Models;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;

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

    public async Task<GoogleSpreadsheetResponse?> GetSpreadsheetAsync(string spreadsheetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);

        var response = await _credentials.Client!.GetAsync($"v4/spreadsheets/{spreadsheetId}", new(), new());
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GoogleSpreadsheetResponse>(payload);
    }

    public async Task<string> CreateSpreadSheetAsync(string spreadSheetTitle)
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
            createResponse = await _credentials.Client.PostAsync("v4/spreadsheets",null, null, request);
            createPayload = await createResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new Exception($"CreateSpreadsheetFailed  exception message : {ex.Message}");
        }

        if (!createResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Google Sheets API responded with status {(int)createResponse.StatusCode} {createResponse.StatusCode}.");
        }

        return createPayload;
    }

    public async Task UpdateHeaders(
        string? defaultSheetTitle,
        string spreadSheetId,
        IList<string> headerValues)
    {
        var targetSheet = string.IsNullOrWhiteSpace(defaultSheetTitle) ? "Sheet1" : defaultSheetTitle!;
        var range = $"{targetSheet}!1:1";
        var updateQuery = new Dictionary<string, string>
        {
            ["valueInputOption"] = "RAW"
        };

        var updateBody = new
        {
            values = new List<IList<string>> { headerValues }
        };

        try
        {
            var updateResponse = await _credentials.Client.PutAsync($"v4/spreadsheets/{spreadSheetId}/values/{Uri.EscapeDataString(range)}", updateQuery, null, updateBody);

            if (!updateResponse.IsSuccessStatusCode)
            {
                var updatePayload = await updateResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Exception($"Failed to apply headers to the new spreadsheet. Status {(int)updateResponse.StatusCode} {updateResponse.StatusCode}");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<GoogleSheetValueRange?> GetSheetValuesAsync(string spreadsheetId, string sheetName, string range = "A:Z")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spreadsheetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);

        var relativeRange = string.IsNullOrEmpty(range) ? sheetName : $"{sheetName}!{range}";
        var response = await _credentials.Client!.GetAsync($"v4/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(relativeRange)}", new(), new());
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
}
