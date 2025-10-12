using Newtonsoft.Json.Linq;
using Ringhel.Procesio.Action.Core.Models;
using System.Text;

namespace CountryAction.Common;

public static class Commons
{
    // Generic fetch helper that takes a delegate performing the HTTP GET
    public static async Task<JArray> FetchAllCountriesAsync(Func<Task<HttpResponseMessage>> doGet)
    {
        var response = await doGet();
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
                    Code = c["cca3"]?.ToString(),
                    Name = c["name"]?["common"]?.ToString(),
                    Population = (long?)c["population"] ?? 0L
                })
                .ToList()
        };
    }

    public static async Task<IEnumerable<string>> ParseCountryCodesFileAsync(FileModel file)
    {
        if (file.File == null || file.File.Length == 0) return Array.Empty<string>();
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
}
