using MVPAPI.WebHook.Application.DTOs.Tokens;
using System.Text;

namespace MVPAPI.WebHook.Application.Common;

public class TokenDecoder
{
    private const char D = '\n';

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
