using System.Text;

namespace MVPAPI.WebHook.Application.Common;

public record DecodedClientToken(
    string BaseUrl,
    string ApiKey,
    string ClientId,
    string ClientSecret,
    string ApplicationName,
    int CompanyId);

public class ClientTokenConverter
{
    private const char D = '\n';

    // Encodes the base URL, API key, client ID, client secret, application name, and company ID into a base64url token.
    public static Result<string> Encode(string baseUrl, string apiKey, string clientId, string clientSecret, string applicationName, string companyId)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return Result<string>.Failure("Base URL is required.");
        if (string.IsNullOrWhiteSpace(apiKey))
            return Result<string>.Failure("API key is required.");
        if (string.IsNullOrWhiteSpace(companyId))
            return Result<string>.Failure("Company id is required.");

        if (baseUrl.StartsWith("https://", StringComparison.Ordinal))
            baseUrl = "S|" + baseUrl.Substring(8);
        else if (baseUrl.StartsWith("http://", StringComparison.Ordinal))
            baseUrl = "H|" + baseUrl.Substring(7);

        var combined = string.Join(D.ToString(),
            new[] { baseUrl, apiKey, clientId, clientSecret, applicationName, companyId });

        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        return Result<string>.Success(token);
    }

    // Validates and decodes the base64url client-token form, surfacing the reason on failure.
    public static Result<DecodedClientToken> Decode(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result<DecodedClientToken>.Failure("Token is required.");

        try
        {
            var encoded = token;
            var pad = encoded.Length % 4;
            if (pad > 0)
                encoded += new string('=', 4 - pad);

            encoded = encoded.Replace('-', '+').Replace('_', '/');

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var parts = decoded.Split(D);

            if (parts.Length < 6)
                return Result<DecodedClientToken>.Failure("Invalid token.");

            var url = parts[0];

            if (url.StartsWith("S|"))
                url = "https://" + url.Substring(2);
            else if (url.StartsWith("H|"))
                url = "http://" + url.Substring(2);

            if (!int.TryParse(parts[5], out var companyId))
                return Result<DecodedClientToken>.Failure("Invalid token.");

            return Result<DecodedClientToken>.Success(new DecodedClientToken(
                BaseUrl: url,
                ApiKey: parts[1],
                ClientId: parts[2],
                ClientSecret: parts[3],
                ApplicationName: parts[4],
                CompanyId: companyId
            ));
        }
        catch
        {
            return Result<DecodedClientToken>.Failure("Invalid token.");
        }
    }
}
