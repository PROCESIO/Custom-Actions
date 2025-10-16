using Common.Helpers;
using Ringhel.Procesio.Action.Core;
using Ringhel.Procesio.Action.Core.ActionDecorators;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Utils;

namespace NoEvents;

[ClassDecorator(Name = "Get Country Data (No Events)", Shape = ActionShape.Square,
    Description = "Educational action without events: predefined options showcase static configuration.",
    Classification = Classification.cat1, IsTestable = true)]
[Permissions(CanDelete = true, CanDuplicate = true, CanAddFromToolbar = true)]
public class CountryWithNoEventsAction : IAction
{
    [FEDecorator(Label = "Global Stats", Type = FeComponentType.DataType, RowId = 1, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public object? GlobalStats { get; set; }

    [FEDecorator(Label = "Region", Type = FeComponentType.Select, RowId = 2, Tab = "Geo",
        Options = nameof(RegionList))]
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = true)]
    public string? Region { get; set; }
    private IList<OptionModel> RegionList { get; set; } = new List<OptionModel>
    {
        new OptionModel { name = "Europe", value = "Europe" },
        new OptionModel { name = "Asia", value = "Asia" },
        new OptionModel { name = "Africa", value = "Africa" },
        new OptionModel { name = "Americas", value = "Americas" },
        new OptionModel { name = "Oceania", value = "Oceania" }
    };

    [FEDecorator(Label = "Country", Type = FeComponentType.Select, RowId = 3, Tab = "Geo",
        Options = nameof(CountryList))]
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = true)]
    public string? Country { get; set; }
    private IList<OptionModel> CountryList { get; set; } = new List<OptionModel>
    {
        new OptionModel { name = "France", value = "FRA" },
        new OptionModel { name = "Germany", value = "DEU" },
        new OptionModel { name = "United States", value = "USA" },
        new OptionModel { name = "Japan", value = "JPN" },
        new OptionModel { name = "Australia", value = "AUS" }
    };

    [FEDecorator(Label = "Currency", Type = FeComponentType.Select, RowId = 4, Tab = "Geo",
        Options = nameof(CurrencyList))]
    [BEDecorator(IOProperty = Direction.Input)]
    [Validator(IsRequired = true)]
    public string? Currency { get; set; }
    private IList<OptionModel> CurrencyList { get; set; } = new List<OptionModel>
    {
        new OptionModel { name = "Euro", value = "EUR" },
        new OptionModel { name = "United States dollar", value = "USD" },
        new OptionModel { name = "Japanese yen", value = "JPY" },
        new OptionModel { name = "Pound sterling", value = "GBP" }
    };

    [FEDecorator(Label = "Region Info", Type = FeComponentType.DataType, RowId = 5, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public object? RegionInfo { get; set; }

    [FEDecorator(Label = "Country Info", Type = FeComponentType.DataType, RowId = 6, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public object? CountryInfo { get; set; }

    [FEDecorator(Label = "Local Time", Type = FeComponentType.Text, RowId = 7, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public string? CountryLocalTime { get; set; }

    [FEDecorator(Label = "Currency Info", Type = FeComponentType.DataType, RowId = 8, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public object? CurrencyInfo { get; set; }

    [FEDecorator(Label = "Country Summary File", Type = FeComponentType.File, RowId = 9, Tab = "Geo")]
    [BEDecorator(IOProperty = Direction.Output)]
    [Validator(IsRequired = false)]
    public FileModel? CountrySummaryFile { get; set; }

    public CountryWithNoEventsAction() { }

    public async Task Execute()
    {
        Validations.ValidateRegion(Region);
        Validations.ValidateCountry(Country);
        Validations.ValidateCurrency(Currency);

        var all = await Commons.FetchAllCountries();
        GlobalStats = Commons.BuildGlobalStats(all);

        var regionCountries = Commons.FilterByRegion(all, Region);
        RegionInfo = Commons.BuildRegionInfo(regionCountries, Region);

        var match = Commons.FindCountryByCode(all, Country);
        if (match != null)
        {
            CountryInfo = Commons.BuildCountryInfo(match);
            CountryLocalTime = Commons.ComputeLocalTime(match["timezones"]?.FirstOrDefault()?.ToString());
            CountrySummaryFile = Commons.BuildCountrySummaryFile(match);
        }

        CurrencyInfo = await Commons.BuildCurrencyInfo(Country, Currency);
    }
}
