using Newtonsoft.Json.Linq;
using Ringhel.Procesio.Action.Core.Models;
using Ringhel.Procesio.Action.Core.Models.Credentials.API;

namespace AllEvents.Helpers;

public static class Commons
{
    // ISO 3166-1 alpha-3 country code key (e.g., USA, FRA, DEU)
    public const string Cca3Key = "cca3";

    // Fetch using plain HttpClient
    public static async Task<JArray> FetchAllCountries()
    {
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("https://restcountries.com/v3.1/all");
        if (!response.IsSuccessStatusCode())
        {
            throw new Exception($"Failed to fetch countries. Status: {response.StatusCode}");
        }
        var payload = await response.Content.ReadAsStringAsync();
        return JArray.Parse(payload);
    }

    // Fetch using APICredentialsManager (validates credentials)
    public static async Task<JArray> FetchAllCountries(APICredentialsManager? credentials)
    {
        Validations.ValidateCredentials(credentials);
        // baseUrl "https://restcountries.com/v3.1" to be set in the credentials
        var response = await credentials!.Client.GetAsync("/all", new(), new());
        if (!response.IsSuccessStatusCode())
        {
            throw new Exception($"Failed to fetch countries. Status: {response.StatusCode}");
        }
        var payload = await response.Content.ReadAsStringAsync();
        return JArray.Parse(payload);
    }

    public static IList<OptionModel> BuildRegions(JArray allCountries)
    {
        var regions = allCountries
            .Select(c => c["region"]?.ToString())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r)
            .ToList();
        return regions.Select(r => new OptionModel { name = r!, value = r! }).ToList();
    }

    public static object BuildGlobalStats(JArray allCountries)
    {
        return new
        {
            CountryCount = allCountries.Count,
            TotalPopulation = allCountries.Sum(c => (long?)c["population"] ?? 0L),
            Top5Populations = allCountries
                .OrderByDescending(c => (long?)c["population"] ?? 0L)
                .Take(5)
                .Select(c => new
                {
                    Code = c[Cca3Key]?.ToString(),
                    Name = c["name"]?["common"]?.ToString(),
                    Population = (long?)c["population"] ?? 0L
                })
                .ToList()
        };
    }

    public static IList<OptionModel> BuildCountryOptions(IEnumerable<JToken> countries)
    {
        var list = new List<OptionModel>();
        foreach (var c in countries)
        {
            var code = c[Cca3Key]?.ToString(); // ISO 3166-1 alpha-3
            var label = c["name"]?["common"]?.ToString();
            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(label))
            {
                list.Add(new OptionModel { name = label, value = code });
            }
        }
        return list;
    }

    public static IList<OptionModel> BuildCurrencyOptions(JObject? currencies)
    {
        var list = new List<OptionModel>();
        if (currencies == null) return list;
        foreach (var prop in currencies.Properties())
        {
            var code = prop.Name; // e.g., "USD", "EUR"
            var label = prop.Value?["name"]?.ToString() ?? code; // e.g., "United States dollar"
            list.Add(new OptionModel { name = label, value = code });
        }
        return list;
    }

    public static object BuildRegionInfo(IEnumerable<JToken> regionCountries, string? region)
    {
        return new
        {
            Region = region,
            CountryCount = regionCountries.Count(),
            TotalPopulation = regionCountries.Sum(c => (long?)c["population"] ?? 0L),
            LargestPopulations = regionCountries
                .OrderByDescending(c => (long?)c["population"] ?? 0L)
                .Take(3)
                .Select(c => new
                {
                    Code = c[Cca3Key]?.ToString(),
                    Name = c["name"]?["common"]?.ToString(),
                    Population = (long?)c["population"] ?? 0L
                })
                .ToList()
        };
    }

    public static object BuildCountryInfo(JToken country)
    {
        return new
        {
            Code = country[Cca3Key]?.ToString(),
            Name = country["name"]?["common"]?.ToString(),
            OfficialName = country["name"]?["official"]?.ToString(),
            Capital = country["capital"]?.FirstOrDefault()?.ToString(),
            Population = (long?)country["population"] ?? 0L,
            Region = country["region"]?.ToString(),
            SubRegion = country["subregion"]?.ToString()
        };
    }

    public static async Task<IEnumerable<string>> ParseCountryCodesFile(FileModel? file)
    {
        if (file?.File == null || file.File.Length == 0) return Array.Empty<string>();
        file.File.Position = 0;
        using var reader = new StreamReader(file.File, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(content)) return Array.Empty<string>();

        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        switch (ext)
        {
            case ".json":
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
                    return Array.Empty<string>();
                }

            case ".csv":
                return content
                    .Split(new[] { '\r', '\n', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            default:
                throw new Exception($"Invalid file extension '{ext}'. Supported extensions are .json and .csv.");
        }
    }

    public static IEnumerable<JToken> FilterByCodesAndRegion(JArray all, IEnumerable<string>? codes, string? region)
    {
        if (codes == null || !codes.Any())
        {
            return FilterByRegion(all, region);
        }
        var set = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
        return all
            .Where(c => set.Contains(c[Cca3Key]?.ToString() ?? string.Empty))
            .Where(c => string.IsNullOrWhiteSpace(region) || string.Equals(c["region"]?.ToString(), region, StringComparison.OrdinalIgnoreCase));
    }

    public static List<JToken> FilterByRegion(JArray all, string? region)
    {
        return all
            .Where(c => string.Equals(c["region"]?.ToString(), region, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static JToken? FindCountryByCode(JArray all, string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return null;
        return all.FirstOrDefault(c => string.Equals(c[Cca3Key]?.ToString(), countryCode, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<object?> BuildCurrencyInfo(string? countryCode, string? currencyCode)
    {
        var all = await FetchAllCountries();
        return BuildCurrencyInfoInternal(all, countryCode, currencyCode);
    }

    public static async Task<object?> BuildCurrencyInfo(APICredentialsManager? credentials, string? countryCode, string? currencyCode)
    {
        var all = await FetchAllCountries(credentials);
        return BuildCurrencyInfoInternal(all, countryCode, currencyCode);
    }

    private static object? BuildCurrencyInfoInternal(JArray all, string? countryCode, string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return new { Code = (string?)null, Name = (string?)null, TimestampUtc = DateTime.UtcNow };
        }

        string? longName = null;
        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            var countryMatch = FindCountryByCode(all, countryCode);
            if (countryMatch?["currencies"] is JObject currForCountry)
            {
                longName = currForCountry[currencyCode]?["name"]?.ToString();
            }
        }
        if (string.IsNullOrWhiteSpace(longName))
        {
            var anyWithCurrency = all.FirstOrDefault(c => (c["currencies"] as JObject)?.Property(currencyCode) != null);
            if (anyWithCurrency?["currencies"] is JObject currObj)
            {
                longName = currObj[currencyCode]?["name"]?.ToString();
            }
        }

        return new
        {
            Code = currencyCode,
            Name = longName,
            TimestampUtc = DateTime.UtcNow
        };
    }

    public static string? ComputeLocalTime(string? tz)
    {
        if (string.IsNullOrWhiteSpace(tz) || !tz.StartsWith("UTC", StringComparison.OrdinalIgnoreCase)) return null;
        var offsetPart = tz.Substring(3);
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

    public static FileModel BuildCountrySummaryFile(JToken country)
    {
        var summary = new
        {
            Code = country[Cca3Key]?.ToString(),
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
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var ms = new MemoryStream(bytes);
        return new FileModel(ms, $"Country_{summary.Code}.json", null);
    }
}
