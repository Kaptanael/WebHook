using MVPAPI.WebHook.Application.DTOs.Tokens;
using System.Text;

namespace MVPAPI.WebHook.Application.Common;

public class TokenDecoder
{
    private const char D = '\n';

    /// <summary>
    /// Inverse of <see cref="Decode"/>: packs the fields into the base64url client-token form
    /// (<c>baseUrl\napiKey\nclientId\nclientSecret\napplicationName\ncompanyId</c>, with the URL scheme
    /// shortened to <c>S|</c>/<c>H|</c>). Used to mint a ClientToken when auto-provisioning a connection.
    /// </summary>
    public static string Encode(string baseUrl, string apiKey, string clientId, string clientSecret, string applicationName, string companyId)
    {
        if (baseUrl.StartsWith("https://", StringComparison.Ordinal))
            baseUrl = "S|" + baseUrl.Substring(8);
        else if (baseUrl.StartsWith("http://", StringComparison.Ordinal))
            baseUrl = "H|" + baseUrl.Substring(7);

        var combined = string.Join(D.ToString(),
            new[] { baseUrl, apiKey, clientId, clientSecret, applicationName, companyId });

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(combined))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static TokenDecoderResponse? Decode(string encoded)
    {
        try
        {
            var pad = encoded.Length % 4;
            if (pad > 0)
                encoded += new string('=', 4 - pad);

            encoded = encoded.Replace('-', '+').Replace('_', '/');

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var parts = decoded.Split(D);

            if (parts.Length < 5)
                return null;

            var url = parts[0];

            if (url.StartsWith("S|"))
                url = "https://" + url.Substring(2);
            else if (url.StartsWith("H|"))
                url = "http://" + url.Substring(2);

            if (!int.TryParse(parts[5], out var companyId))
                return null;

            return new TokenDecoderResponse(
                BaseUrl: url,
                ApiKey: parts[1],
                ClientId: parts[2],
                ClientSecret: parts[3],
                ApplicationName: parts[4],
                CompanyId: companyId
            );
        }
        catch
        {
            return null;
        }
    }
}
