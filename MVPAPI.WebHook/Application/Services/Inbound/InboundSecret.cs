using System.Security.Cryptography;
using System.Text;

namespace MVPAPI.WebHook.Application.Services.Inbound;

internal static class InboundSecret
{
    /// <summary>Compares two secrets in constant time so a match can't be discovered by timing.</summary>
    public static bool FixedTimeEquals(string expected, string actual) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(actual));
}
