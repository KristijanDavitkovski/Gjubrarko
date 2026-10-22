using System.Security.Cryptography;
using System.Text;

namespace Gjubrarko.Application.Common;

public static class AnonymousToken
{
    public static string Create() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool Matches(string token, string? hash) =>
        hash is not null && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(token)), Encoding.ASCII.GetBytes(hash));
}
