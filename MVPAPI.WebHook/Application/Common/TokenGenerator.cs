using System.Security.Cryptography;

namespace MVPAPI.WebHook.Application.Common;

public static class TokenGenerator
{
    public static string Generate(int byteLength = 48)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
