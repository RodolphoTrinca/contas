using System.Security.Cryptography;
using System.Text.Json;

namespace Contas.Application;

internal static class Fingerprint
{
    internal static string For(object input) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(input)));
}
