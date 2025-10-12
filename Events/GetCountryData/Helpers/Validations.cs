namespace CountryAction.Common;

public static class Validations
{
    public static void ValidateRegion(string? region)
    {
        if (string.IsNullOrWhiteSpace(region))
        {
            throw new Exception("Region is required.");
        }
    }

    public static void ValidateCountry(string? country)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new Exception("Country is required.");
        }
    }

    public static void ValidateCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new Exception("Currency is required.");
        }
    }

    public static void ValidateCredentials(object? client, object? config)
    {
        if (client == null || config == null)
        {
            throw new Exception("Invalid REST Credentials instance.");
        }
    }
}
