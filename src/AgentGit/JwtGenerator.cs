using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentGit;

internal static class JwtGenerator
{
    internal static string Generate(string clientId, string privateKeyPath)
    {
        string pemText = File.ReadAllText(privateKeyPath);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pemText);

        long iat = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeSeconds();
        long exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();

        string header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
            new JwtHeader { Alg = "RS256", Typ = "JWT" },
            JwtJsonContext.Default.JwtHeader));

        string payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
            new JwtPayload { Iat = iat, Exp = exp, Iss = clientId },
            JwtJsonContext.Default.JwtPayload));

        byte[] signature = rsa.SignData(
            Encoding.UTF8.GetBytes($"{header}.{payload}"),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return $"{header}.{payload}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

internal sealed class JwtHeader
{
    [JsonPropertyName("alg")]
    public required string Alg { get; init; }

    [JsonPropertyName("typ")]
    public required string Typ { get; init; }
}

internal sealed class JwtPayload
{
    [JsonPropertyName("iat")]
    public long Iat { get; init; }

    [JsonPropertyName("exp")]
    public long Exp { get; init; }

    [JsonPropertyName("iss")]
    public required string Iss { get; init; }
}

[JsonSerializable(typeof(JwtHeader))]
[JsonSerializable(typeof(JwtPayload))]
internal partial class JwtJsonContext : JsonSerializerContext;
