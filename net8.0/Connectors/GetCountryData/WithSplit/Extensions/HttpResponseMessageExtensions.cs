public static class HttpResponseMessageExtensions
{
    public static bool IsSuccessStatusCode(this HttpResponseMessage? apiResponse)
    {
        if (apiResponse is null)
        {
            return false;
        }
        var statusCode = (int)apiResponse.StatusCode;
        return statusCode is >= 200 and <= 299;
    }
}
