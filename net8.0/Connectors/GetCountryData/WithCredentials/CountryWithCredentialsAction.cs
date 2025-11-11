using Newtonsoft.Json.Linq;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;
using Ringhel.Procesio.Action.Core.Utils;
using WithCredentials.Helpers;

namespace WithCredentials;

[ClassDecorator(Name = "Get Country Data (Credentials)", Shape = ActionShape.Square,
    Description = "Educational action using new events feature with required REST credentials.",
    Classification = Classification.cat1, IsTestable = true)]
[Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
public class CountryWithCredentialsAction : IAction
{
    #region Properties

    [FEDecorator(Label = "REST Credentials", Type = FeComponentType.Credentials_Rest, RowId = 0, Tab = "Geo",
        Tooltip = "Provide REST credentials; base URL should be https://restcountries.com/v3.1")]
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = true)]
    public APICredentialsManager? Credentials { get; set; }

    [FEDecorator(Label = "Global Stats", Type = FeComponentType.DataType, RowId = 1, Tab = "Geo",
        Tooltip = "Aggregated statistics over all countries (computed after credentials are selected).")]
    [BEDecorator(IOProperty = Direction.Output)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public object? GlobalStats { get; set; }

    [FEDecorator(Label = "Region", Type = FeComponentType.Select, RowId = 2, Tab = "Geo",
        Options = nameof(RegionList), Tooltip = "Select a geographic region (e.g., Europe, Asia).")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = true)]
    public string? Region { get; set; }
    private IList<OptionModel> RegionList { get; set; } = new List<OptionModel>();

    [FEDecorator(Label = "Country Codes File", Type = FeComponentType.File, RowId = 3, Tab = "Geo",
        Tooltip = "Optional file (CSV or JSON array) listing ISO alpha-3 country codes to restrict the Country list.")]
    [BEDecorator(IOProperty = Direction.Input)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Region), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public FileModel? CountryCodesFile { get; set; }

    [FEDecorator(Label = "Country", Type = FeComponentType.Select, RowId = 4, Tab = "Geo",
        Options = nameof(CountryList), Tooltip = "Select a country. Filtered by Region and/or uploaded codes.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Region), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = true)]
    public string? Country { get; set; }
    private IList<OptionModel> CountryList { get; set; } = new List<OptionModel>();

    [FEDecorator(Label = "Currency", Type = FeComponentType.Select, RowId = 5, Tab = "Geo",
        Options = nameof(CurrencyList), Tooltip = "Currencies of the selected country.")]
    [BEDecorator(IOProperty = Direction.InputOutput)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = true)]
    public string? Currency { get; set; }
    private IList<OptionModel> CurrencyList { get; set; } = new List<OptionModel>();

    [FEDecorator(Label = "Refresh", Type = FeComponentType.Button, RowId = 6, Tab = "Geo",
        Tooltip = "Press to re-run initialization (credentials-driven) without recreating the action instance.")]
    [BEDecorator(IOProperty = Direction.Input)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(GlobalStats), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Region), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public bool Refresh { get; set; }

    [FEDecorator(Label = "Region Info", Type = FeComponentType.DataType, RowId = 7, Tab = "Geo",
        Tooltip = "Summary information for the selected region.")]
    [BEDecorator(IOProperty = Direction.Output)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Region), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public object? RegionInfo { get; set; }

    [FEDecorator(Label = "Country Info", Type = FeComponentType.DataType, RowId = 8, Tab = "Geo",
        Tooltip = "Detailed information for the selected country.")]
    [BEDecorator(IOProperty = Direction.Output)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public object? CountryInfo { get; set; }

    [FEDecorator(Label = "Local Time", Type = FeComponentType.Text, RowId = 9, Tab = "Geo",
        Tooltip = "Current local time (approx) in the selected country's first reported timezone.")]
    [BEDecorator(IOProperty = Direction.Output)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public string? CountryLocalTime { get; set; }

    [FEDecorator(Label = "Currency Info", Type = FeComponentType.DataType, RowId = 10, Tab = "Geo",
        Tooltip = "Information about the selected currency.")]
    [BEDecorator(IOProperty = Direction.Output)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Currency), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public object? CurrencyInfo { get; set; }

    [FEDecorator(Label = "Country Summary File", Type = FeComponentType.File, RowId = 11, Tab = "Geo",
        Tooltip = "Generated JSON summary for the selected country.")]
    [BEDecorator(IOProperty = Direction.Output)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Credentials), Operator = Operator.NotEquals, Value = null)]
    [DependencyDecorator(Tab = "Geo", Control = nameof(Country), Operator = Operator.NotEquals, Value = null)]
    [Validator(IsRequired = false)]
    public FileModel? CountrySummaryFile { get; set; }

    #endregion

    public CountryWithCredentialsAction() { }

    public Task Execute()
    {
        Validations.ValidateRegion(Region);
        Validations.ValidateCountry(Country);
        Validations.ValidateCurrency(Currency);
        return Task.CompletedTask;
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Credentials),
        InputControls = [nameof(Credentials)],
        OutputControls = [nameof(Region)],
        OutputTarget = OutputTarget.Options)]
    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Credentials),
        InputControls = [nameof(Credentials)],
        OutputControls = [nameof(GlobalStats)],
        OutputTarget = OutputTarget.Value)]
    public async Task InitializeData()
    {
        var all = await Commons.FetchAllCountries(Credentials);
        RegionList = Commons.BuildRegions(all);
        GlobalStats = Commons.BuildGlobalStats(all);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnClick,
        TriggerControl = nameof(Refresh),
        InputControls = [nameof(Credentials)],
        OutputControls = [nameof(GlobalStats)],
        OutputTarget = OutputTarget.Value)]
    [ControlEventHandler(
        EventType = ControlEventType.OnClick,
        TriggerControl = nameof(Refresh),
        InputControls = [nameof(Credentials)],
        OutputControls = [nameof(Region)],
        OutputTarget = OutputTarget.Options)]
    public async Task OnRefreshPressed()
    {
        await InitializeData();
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Region),
        InputControls = [nameof(Credentials), nameof(Region)],
        OutputControls = [nameof(Country)],
        OutputTarget = OutputTarget.Options)]
    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Region),
        InputControls = [nameof(Credentials), nameof(Region)],
        OutputControls = [nameof(RegionInfo)],
        OutputTarget = OutputTarget.Value)]
    public async Task OnRegionChange()
    {
        Validations.ValidateRegion(Region);
        var all = await Commons.FetchAllCountries(Credentials);
        var regionCountries = Commons.FilterByRegion(all, Region);
        CountryList = Commons.BuildCountryOptions(regionCountries);
        RegionInfo = Commons.BuildRegionInfo(regionCountries, Region);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(CountryCodesFile),
        InputControls = [nameof(Credentials), nameof(CountryCodesFile), nameof(Region)],
        OutputControls = [nameof(Country)],
        OutputTarget = OutputTarget.Options)]
    public async Task OnCountryCodesFileChange()
    {
        var all = await Commons.FetchAllCountries(Credentials);
        var codes = await Commons.ParseCountryCodesFile(CountryCodesFile);
        var filtered = Commons.FilterByCodesAndRegion(all, codes, Region);
        CountryList = Commons.BuildCountryOptions(filtered);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Country),
        InputControls = [nameof(Credentials), nameof(Country)],
        OutputControls = [nameof(CountrySummaryFile), nameof(CountryInfo), nameof(CountryLocalTime)],
        OutputTarget = OutputTarget.Value)]
    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Country),
        InputControls = [nameof(Credentials), nameof(Country)],
        OutputControls = [nameof(Currency)],
        OutputTarget = OutputTarget.Options)]
    public async Task OnCountryChange()
    {
        Validations.ValidateCountry(Country);

        var all = await Commons.FetchAllCountries(Credentials);
        var match = Commons.FindCountryByCode(all, Country);
        if (match == null) return;

        CountryInfo = Commons.BuildCountryInfo(match);
        CountryLocalTime = Commons.ComputeLocalTime(match["timezones"]?.FirstOrDefault()?.ToString());
        CountrySummaryFile = Commons.BuildCountrySummaryFile(match);
        CurrencyList = Commons.BuildCurrencyOptions(match["currencies"] as JObject);
    }

    [ControlEventHandler(
        EventType = ControlEventType.OnChange,
        TriggerControl = nameof(Currency),
        InputControls = [nameof(Credentials), nameof(Country), nameof(Currency)],
        OutputControls = [nameof(CurrencyInfo)],
        OutputTarget = OutputTarget.Value)]
    public async Task OnCurrencyChange()
    {
        Validations.ValidateCurrency(Currency);
        CurrencyInfo = await Commons.BuildCurrencyInfo(Credentials, Country, Currency);
    }
}
