using System.Globalization;
using System.Text.Json;
using GoogleSheetsAction.Models;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;

namespace GoogleSheetsAction.Services;

public sealed class GoogleSheetsClient
{
    private readonly APICredentialsManager _credentials;

    public GoogleSheetsClient(APICredentialsManager credentials)
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
