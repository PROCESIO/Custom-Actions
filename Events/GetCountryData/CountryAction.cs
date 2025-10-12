using CountryAction.Common;
using Newtonsoft.Json.Linq;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;
using System.Text;

namespace CountryAction;

/// <summary>
/// Demonstration connector showcasing:
///  - OnLoad initialization (EventType.OnLoad)
///  - Chained dynamic option population (Region -> Country -> Currency)
///  - File input influencing option lists (CountryCodesFile)
///  - Multiple ControlEventHandler attributes on same trigger (Options + Value)
///  - OutputTarget.Value vs OutputTarget.Options
///  - Generation of an output file (CountrySummaryFile)
///  - Re-initialization via a Refresh trigger
///  - Local time computation for selected country (first timezone offset)
/// Free API used: https://restcountries.com/v3.1
/// </summary>
[ClassDecorator(Name = "Get Country Data", Shape = ActionShape.Square,
    Description = "Educational connector: loads countries, filters by region, exposes country + currency data.",
    Classification = Classification.cat1, IsTestable = true)]
[Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
public class CountryAction : IAction
{
    #region Properties

    [FEDecorator(Label = "Region", Type = FeComponentType.Select, RowId = 2, Tab = "Geo",
        Options = nameof(RegionList), Tooltip = "Select a geographic region (e.g., Europe, Asia).")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [Validator(IsRequired = false)]
    public string? Region { get; set; }
    private IList<OptionModel> RegionList { get; set; } = new List<OptionModel>();

    [FEDecorator(Label = "Country Codes File", Type = FeComponentType.File, RowId = 3, Tab = "Geo",
        Tooltip = "Optional file (CSV or JSON array) listing ISO alpha-3 country codes to restrict the Country list.")]
    [BEDecorator(IOProperty = Direction.Input)]
    public FileModel? CountryCodesFile { get; set; }

    [FEDecorator(Label = "Country", Type = FeComponentType.Select, RowId = 4, Tab = "Geo",
        Options = nameof(CountryList), Tooltip = "Select a country. Filtered by Region and/or uploaded codes.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Region), Operator = Operator.NotEquals, Value = null)]
    public string? Country { get; set; }
    private IList<OptionModel> CountryList { get; set; } = new List<OptionModel>();

    [FEDecorator(Label = "Currency", Type = FeComponentType.Select, RowId = 5, Tab = "Geo",
        Options = nameof(CurrencyList), Tooltip = "Currencies of the selected country.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
    public string? Currency { get; set; }
    private IList<OptionModel> CurrencyList { get; set; } = new List<OptionModel>();

    [FEDecorator(Label = "Global Stats", Type = FeComponentType.DataType, RowId = 7, Tab = "Geo",
        Tooltip = "Aggregated statistics over all countries (computed on load or refresh).")]
    [BEDecorator(IOProperty = Direction.Output)]
    public object? GlobalStats { get; set; }

    [FEDecorator(Label = "Refresh", Type = FeComponentType.Button, RowId = 8, Tab = "Geo",
        Tooltip = "Press to re-run initialization (OnLoad logic) without recreating the action instance.")]
    [BEDecorator(IOProperty = Direction.Input)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(GlobalStats), Operator = Operator.NotEquals, Value = null)]
    public bool Refresh { get; set; }

    [FEDecorator(Label = "Region Info", Type = FeComponentType.DataType, RowId = 9, Tab = "Geo",
        Tooltip = "Summary information for the selected region.")]
    [BEDecorator(IOProperty = Direction.Output)]
    public object? RegionInfo { get; set; }

    [FEDecorator(Label = "Country Info", Type = FeComponentType.DataType, RowId = 10, Tab = "Geo",
        Tooltip = "Detailed information for the selected country.")]
    [BEDecorator(IOProperty = Direction.Output)]
    public object? CountryInfo { get; set; }

    [FEDecorator(Label = "Local Time", Type = FeComponentType.Text, RowId = 11, Tab = "Geo",
        Tooltip = "Current local time (approx) in the selected country's first reported timezone.")]
    [BEDecorator(IOProperty = Direction.Output)]
    public string? CountryLocalTime { get; set; }

    [FEDecorator(Label = "Currency Aggregate", Type = FeComponentType.DataType, RowId = 12, Tab = "Geo",
        Tooltip = "Information about the selected currency.")]
    [BEDecorator(IOProperty = Direction.Output)]
    public object? CurrencyAggregate { get; set; }

    [FEDecorator(Label = "Country Summary File", Type = FeComponentType.File, RowId = 13, Tab = "Geo",
        Tooltip = "Generated JSON summary for the selected country.")]
    [BEDecorator(IOProperty = Direction.Output)]
    public FileModel? CountrySummaryFile { get; set; }

    #endregion

    public CountryAction() { }

    #region Execution
    public async Task Execute()
    {
        ValidateRegion();
        ValidateCountry();
    }
    #endregion

    #region Event Handlers

    // ON LOAD: initialize master data (Options + Value via two attributes)
    [ControlEventHandler(EventType = ControlEventType.OnLoad, OutputTarget = OutputTarget.Options,
        OutputControls = [nameof(Region)])]
    [ControlEventHandler(EventType = ControlEventType.OnLoad, OutputTarget = OutputTarget.Value,
        OutputControls = [nameof(GlobalStats)])]
    public async Task InitializeData()
    {
        var all = await FetchAllCountries();
        BuildRegions(all);
        BuildGlobalStats(all);
    }

    // Refresh button re-runs initialization logic (Value + Options outputs)
    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(Refresh), OutputTarget = OutputTarget.Value,
        OutputControls = [nameof(GlobalStats)])]
    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(Refresh), OutputTarget = OutputTarget.Options,
        OutputControls = [nameof(Region)], Order = 1)]
    public async Task OnRefreshToggle()
    {
        await InitializeData();
    }

    // Region changed: populate countries (Options) then region info (Value)
    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(Region), OutputTarget = OutputTarget.Options,
        InputControls = [nameof(Region)], OutputControls = [nameof(Country)])]
    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(Region), OutputTarget = OutputTarget.Value,
        InputControls = [nameof(Region)], OutputControls = [nameof(RegionInfo)], Order = 1)]
    public async Task OnRegionChange()
    {
        ValidateRegion();
        var all = await FetchAllCountries();

        var regionCountries = all
            .Where(c => string.Equals(c["region"]?.ToString(), Region, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var countries = new List<OptionModel>();
        foreach (var c in regionCountries)
        {
            var name = c["cca3"]?.ToString(); // ISO alpha-3 code
            var label = c["name"]?["common"]?.ToString();
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(label))
            {
                countries.Add(new OptionModel { name = label, value = name });
            }
        }
        CountryList = countries;

        RegionInfo = new
        {
            Region,
            CountryCount = regionCountries.Count,
            TotalPopulation = regionCountries.Sum(c => (long?)c["population"] ?? 0L),
            LargestPopulations = regionCountries
                .OrderByDescending(c => (long?)c["population"] ?? 0L)
                .Take(3)
                .Select(c => new
                {
                    Code = c["cca3"]?.ToString(),
                    Name = c["name"]?["common"]?.ToString(),
                    Population = (long?)c["population"] ?? 0L
                })
                .ToList()
        };
    }

    // Country codes file uploaded -> filter Country list
    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(CountryCodesFile), OutputTarget = OutputTarget.Options,
        InputControls = [nameof(CountryCodesFile), nameof(Region)], OutputControls = [nameof(Country)])]
    public async Task OnCountryCodesFileChange()
    {
        var all = await FetchAllCountries();

        if (CountryCodesFile?.File == null || CountryCodesFile.File.Length == 0)
        {
            return; // nothing to process
        }

        var codes = await ParseCountryCodesFile(CountryCodesFile);
        if (!codes.Any()) return;

        var filtered = all
            .Where(c => codes.Contains(c["cca3"]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            .Where(c => string.IsNullOrWhiteSpace(Region) || string.Equals(c["region"]?.ToString(), Region, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var countries = new List<OptionModel>();
        foreach (var c in filtered)
        {
            var code = c["cca3"]?.ToString();
            var label = c["name"]?["common"]?.ToString();
            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(label))
            {
                countries.Add(new OptionModel { name = label, value = code });
            }
        }
        CountryList = countries;
    }

    // Country changed -> country info (Value), currencies (Options), summary file (Value), local time (Value)
    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(Country), OutputTarget = OutputTarget.Value,
        InputControls = [nameof(Country)], OutputControls = [nameof(CountryInfo), nameof(CountryLocalTime)], Order = 0)]
    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(Country), OutputTarget = OutputTarget.Options,
        InputControls = [nameof(Country)], OutputControls = [nameof(Currency)], Order = 1)]
    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(Country), OutputTarget = OutputTarget.Value,
        InputControls = [nameof(Country)], OutputControls = [nameof(CountrySummaryFile)], Order = 2)]
    public async Task OnCountryChange()
    {
        Validations.ValidateCountry(Country);

        var all = await Commons.FetchAllCountriesAsync(() => new HttpClient().GetAsync("https://restcountries.com/v3.1/all"));

        var match = all.FirstOrDefault(c => string.Equals(c["cca3"]?.ToString(), Country, StringComparison.OrdinalIgnoreCase));
        if (match == null) return;

        CountryInfo = new
        {
            Code = match["cca3"]?.ToString(),
            Name = match["name"]?["common"]?.ToString(),
            OfficialName = match["name"]?["official"]?.ToString(),
            Capital = match["capital"]?.FirstOrDefault()?.ToString(),
            Population = (long?)match["population"] ?? 0L,
            Region = match["region"]?.ToString(),
            SubRegion = match["subregion"]?.ToString()
        };

        var currencies = new List<OptionModel>();
        if (match["currencies"] is JObject currObj)
        {
            foreach (var prop in currObj.Properties())
            {
                var code = prop.Name;
                var label = prop.Value?["name"]?.ToString() ?? code;
                currencies.Add(new OptionModel { name = label, value = code });
            }
        }
        CurrencyList = currencies;

        CountryLocalTime = Commons.ComputeLocalTime(match["timezones"]?.FirstOrDefault()?.ToString());
        CountrySummaryFile = Commons.BuildCountrySummaryFile(match);
    }

    // Currency changed -> aggregate info (Value)
    [ControlEventHandler(EventType = ControlEventType.OnChange, TriggerControl = nameof(Currency), OutputTarget = OutputTarget.Value,
        InputControls = [nameof(Currency)], OutputControls = [nameof(CurrencyAggregate)])]
    public void OnCurrencyChange()
    {
        Validations.ValidateCurrency(Currency);
        CurrencyAggregate = new
        {
            SelectedCurrency = Currency,
            TimestampUtc = DateTime.UtcNow
        };
    }

    #endregion

    #region Helpers

    private async Task<JArray> FetchAllCountries()
    {
        var response = await new HttpClient().GetAsync("https://restcountries.com/v3.1/all");
        if (!response.IsSuccessStatusCode())
        {
            throw new Exception($"Failed to fetch countries. Status: {response.StatusCode}");
        }
        var payload = await response.Content.ReadAsStringAsync();
        return JArray.Parse(payload);
    }

    private void BuildRegions(JArray allCountries)
    {
        var regions = allCountries
            .Select(c => c["region"]?.ToString())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r)
            .ToList();
        RegionList = regions.Select(r => new OptionModel { name = r!, value = r! }).ToList();
    }

    private void BuildGlobalStats(JArray allCountries)
    {
        GlobalStats = new
        {
            CountryCount = allCountries.Count,
            TotalPopulation = allCountries.Sum(c => (long?)c["population"] ?? 0L),
            Top5Populations = allCountries
                .OrderByDescending(c => (long?)c["population"] ?? 0L)
                .Take(5)
                .Select(c => new
                {
                    Code = c["cca3"]?.ToString(),
                    Name = c["name"]?["common"]?.ToString(),
                    Population = (long?)c["population"] ?? 0L
                })
                .ToList()
        };
    }

    private static async Task<IEnumerable<string>> ParseCountryCodesFile(FileModel file)
    {
        if (file.File == null || file.File.Length == 0) return Array.Empty<string>();
        file.File.Position = 0;
        using var reader = new StreamReader(file.File, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(content)) return Array.Empty<string>();

        var ext = Path.GetExtension(file.Name).ToLowerInvariant();

        if (ext == ".json")
        {
            try
            {
                var arr = JArray.Parse(content);
                return arr
                    .Select(t => t.ToString().Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                // Invalid JSON content
                return Array.Empty<string>();
            }
        }
        else if (ext == ".csv")
        {
            var tokens = content
                .Split(new[] { '\r', '\n', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return tokens;
        }
        else
        {
            throw new Exception($"Invalid file extension '{ext}'. Supported extensions are .json and .csv.");
        }
    }

    private static string? ComputeLocalTime(string? tz)
    {
        if (string.IsNullOrWhiteSpace(tz) || !tz.StartsWith("UTC", StringComparison.OrdinalIgnoreCase)) return null;
        // Examples: "UTC+05:30", "UTC-04:00", "UTC+01:00"
        var offsetPart = tz.Substring(3); // +05:30
        if (offsetPart.Length == 0) return DateTime.UtcNow.ToString("o");
        try
        {
            var sign = offsetPart[0];
            var span = offsetPart.Substring(1);
            var parts = span.Split(':');
            if (parts.Length == 0) return null;
            var hours = int.Parse(parts[0]);
            var minutes = parts.Length > 1 ? int.Parse(parts[1]) : 0;
            var offset = new TimeSpan(hours, minutes, 0);
            if (sign == '-') offset = -offset;
            return (DateTime.UtcNow + offset).ToString("o");
        }
        catch { return null; }
    }

    private FileModel BuildCountrySummaryFile(JToken country)
    {
        var summary = new
        {
            Code = country["cca3"]?.ToString(),
            Name = country["name"]?["common"]?.ToString(),
            OfficialName = country["name"]?["official"]?.ToString(),
            Capital = country["capital"]?.FirstOrDefault()?.ToString(),
            Region = country["region"]?.ToString(),
            SubRegion = country["subregion"]?.ToString(),
            Population = (long?)country["population"] ?? 0L,
            Timezones = country["timezones"]?.Select(t => t.ToString()).ToList(),
            Currencies = (country["currencies"] as JObject)?.Properties().Select(p => p.Name).ToList()
        };
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(summary, Newtonsoft.Json.Formatting.Indented);
        var bytes = Encoding.UTF8.GetBytes(json);
        var ms = new MemoryStream(bytes);
        return new FileModel(ms, $"Country_{summary.Code}.json", null);
    }

    #endregion

    #region Validations

    private void ValidateRegion()
    {
        if (string.IsNullOrWhiteSpace(Region))
        {
            throw new Exception("Region is required.");
        }
    }

    private void ValidateCountry()
    {
        if (string.IsNullOrWhiteSpace(Country))
        {
            throw new Exception("Country is required.");
        }
    }

    #endregion
}
