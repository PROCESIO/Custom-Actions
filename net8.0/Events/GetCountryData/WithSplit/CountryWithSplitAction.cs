using Common.Helpers;
using Newtonsoft.Json.Linq;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;

namespace WithSplit;

[ClassDecorator(Name = "Get Country Data (Split)", Shape = ActionShape.Square,
    Description = "Same as CountryAction but methods are split per output for showcasing ordering control.",
    Classification = Classification.cat1, IsTestable = true)]
[Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
public class CountryWithSplitAction : IAction
{
    [FEDecorator(Label = "Global Stats", Type = FeComponentType.DataType, RowId = 1, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public object? GlobalStats { get; set; }

    [FEDecorator(Label = "Region", Type = FeComponentType.Select, RowId = 2, Tab = "Geo",
        Options = nameof(RegionList))]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [Validator(IsRequired = true)]
    public string? Region { get; set; }
    private IList<OptionModel> RegionList { get; set; } = new List<OptionModel>();

    [FEDecorator(Label = "Country Codes File", Type = FeComponentType.File, RowId = 3, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Input)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Region), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public FileModel? CountryCodesFile { get; set; }

    [FEDecorator(Label = "Country", Type = FeComponentType.Select, RowId = 4, Tab = "Geo",
        Options = nameof(CountryList))]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Region), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = true)]
    public string? Country { get; set; }
    private IList<OptionModel> CountryList { get; set; } = new List<OptionModel>();

    [FEDecorator(Label = "Currency", Type = FeComponentType.Select, RowId = 5, Tab = "Geo",
        Options = nameof(CurrencyList))]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = true)]
    public string? Currency { get; set; }
    private IList<OptionModel> CurrencyList { get; set; } = new List<OptionModel>();

    [FEDecorator(Label = "Refresh", Type = FeComponentType.Button, RowId = 6, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Input)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(GlobalStats), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Region), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public bool Refresh { get; set; }

    [FEDecorator(Label = "Region Info", Type = FeComponentType.DataType, RowId = 7, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Region), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public object? RegionInfo { get; set; }

    [FEDecorator(Label = "Country Info", Type = FeComponentType.DataType, RowId = 8, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public object? CountryInfo { get; set; }

    [FEDecorator(Label = "Local Time", Type = FeComponentType.Text, RowId = 9, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public string? CountryLocalTime { get; set; }

    [FEDecorator(Label = "Currency Info", Type = FeComponentType.DataType, RowId = 10, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Currency), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public object? CurrencyInfo { get; set; }

    [FEDecorator(Label = "Country Summary File", Type = FeComponentType.File, RowId = 11, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public FileModel? CountrySummaryFile { get; set; }

    public Task Execute()
    {
        Validations.ValidateRegion(Region);
        Validations.ValidateCountry(Country);
        Validations.ValidateCurrency(Currency);
        return Task.CompletedTask;
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnLoad,
        OutputControls = [nameof(Region)],
        OutputTarget = OutputTarget.Options)]
    public async Task InitRegions()
    {
        var all = await Commons.FetchAllCountries();
        RegionList = Commons.BuildRegions(all);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnLoad,
        OutputControls = [nameof(GlobalStats)],
        OutputTarget = OutputTarget.Value)]
    public async Task InitStats()
    {
        var all = await Commons.FetchAllCountries();
        GlobalStats = Commons.BuildGlobalStats(all);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnClick,
        TriggerControl = nameof(Refresh),
        OutputControls = [nameof(GlobalStats)],
        OutputTarget = OutputTarget.Value,
        Order = 0)]
    public async Task RefreshStats()
    {
        await InitStats();
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnClick,
        TriggerControl = nameof(Refresh),
        OutputControls = [nameof(Region)],
        OutputTarget = OutputTarget.Options,
        Order = 1)]
    public async Task RefreshRegions()
    {
        await InitRegions();
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Region),
        InputControls = [nameof(Region)],
        OutputControls = [nameof(Country)],
        OutputTarget = OutputTarget.Options,
        Order = 0)]
    public async Task PopulateCountries()
    {
        var all = await Commons.FetchAllCountries();
        var regionCountries = Commons.FilterByRegion(all, Region);
        CountryList = Commons.BuildCountryOptions(regionCountries);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Region),
        InputControls = [nameof(Region)],
        OutputControls = [nameof(RegionInfo)],
        OutputTarget = OutputTarget.Value,
        Order = 1)]
    public async Task PopulateRegionInfo()
    {
        var all = await Commons.FetchAllCountries();
        var regionCountries = Commons.FilterByRegion(all, Region);
        RegionInfo = Commons.BuildRegionInfo(regionCountries, Region);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(CountryCodesFile),
        InputControls = [nameof(CountryCodesFile), nameof(Region)],
        OutputControls = [nameof(Country)],
        OutputTarget = OutputTarget.Options)]
    public async Task PopulateCountriesFromFile()
    {
        var all = await Commons.FetchAllCountries();
        var codes = await Commons.ParseCountryCodesFile(CountryCodesFile);
        var filtered = Commons.FilterByCodesAndRegion(all, codes, Region);
        CountryList = Commons.BuildCountryOptions(filtered);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Country),
        InputControls = [nameof(Country)],
        OutputControls = [nameof(CountryInfo)],
        OutputTarget = OutputTarget.Value,
        Order = 0)]
    public async Task PopulateCountryInfo()
    {
        var all = await Commons.FetchAllCountries();
        var match = Commons.FindCountryByCode(all, Country);
        if (match == null) { CountryInfo = null; return; }
        CountryInfo = Commons.BuildCountryInfo(match);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Country),
        InputControls = [nameof(Country)],
        OutputControls = [nameof(CountrySummaryFile)],
        OutputTarget = OutputTarget.Value,
        Order = 1)]
    public async Task PopulateCountrySummary()
    {
        var all = await Commons.FetchAllCountries();
        var match = Commons.FindCountryByCode(all, Country);
        if (match == null) { CountrySummaryFile = null; return; }
        CountrySummaryFile = Commons.BuildCountrySummaryFile(match);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Country),
        InputControls = [nameof(Country)],
        OutputControls = [nameof(CountryLocalTime)],
        OutputTarget = OutputTarget.Value,
        Order = 2)]
    public async Task PopulateCountryLocalTime()
    {
        var all = await Commons.FetchAllCountries();
        var match = Commons.FindCountryByCode(all, Country);
        if (match == null) { CountryLocalTime = null; return; }
        CountryLocalTime = Commons.ComputeLocalTime(match["timezones"]?.FirstOrDefault()?.ToString());
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Country),
        InputControls = [nameof(Country)],
        OutputControls = [nameof(Currency)],
        OutputTarget = OutputTarget.Options,
        Order = 3)]
    public async Task PopulateCurrencies()
    {
        var all = await Commons.FetchAllCountries();
        var match = Commons.FindCountryByCode(all, Country);
        CurrencyList = Commons.BuildCurrencyOptions(match?["currencies"] as JObject);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Currency),
        InputControls = [nameof(Country), nameof(Currency)],
        OutputControls = [nameof(CurrencyInfo)],
        OutputTarget = OutputTarget.Value)]
    public async Task OnCurrencyChange()
    {
        Validations.ValidateCurrency(Currency);
        CurrencyInfo = await Commons.BuildCurrencyInfo(Country, Currency);
    }
}
