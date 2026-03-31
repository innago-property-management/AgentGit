using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace AgentGit.Tests;

public class JwtGeneratorTests : IDisposable
{
    private readonly string _keyPath;

    public JwtGeneratorTests()
    {
        using var rsa = RSA.Create(2048);
        string pem = rsa.ExportRSAPrivateKeyPem();
        _keyPath = Path.GetTempFileName();
        File.WriteAllText(_keyPath, pem);
    }

    [Fact]
    public void Generate_returns_three_part_jwt()
    {
        string jwt = JwtGenerator.Generate("test-client-id", _keyPath);

        jwt.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void Generate_header_contains_rs256_algorithm()
    {
        string jwt = JwtGenerator.Generate("test-client-id", _keyPath);

        string header = jwt.Split('.')[0];
        byte[] headerBytes = Base64UrlDecode(header);
        using var doc = JsonDocument.Parse(headerBytes);

        doc.RootElement.GetProperty("alg").GetString().Should().Be("RS256");
        doc.RootElement.GetProperty("typ").GetString().Should().Be("JWT");
    }

    [Fact]
    public void Generate_payload_contains_client_id_as_issuer()
    {
        string jwt = JwtGenerator.Generate("my-client-id", _keyPath);

        string payload = jwt.Split('.')[1];
        byte[] payloadBytes = Base64UrlDecode(payload);
        using var doc = JsonDocument.Parse(payloadBytes);

        doc.RootElement.GetProperty("iss").GetString().Should().Be("my-client-id");
    }

    [Fact]
    public void Generate_payload_has_valid_iat_and_exp()
    {
        long before = DateTimeOffset.UtcNow.AddSeconds(-120).ToUnixTimeSeconds();
        string jwt = JwtGenerator.Generate("test-client-id", _keyPath);
        long after = DateTimeOffset.UtcNow.AddMinutes(11).ToUnixTimeSeconds();

        string payload = jwt.Split('.')[1];
        byte[] payloadBytes = Base64UrlDecode(payload);
        using var doc = JsonDocument.Parse(payloadBytes);

        long iat = doc.RootElement.GetProperty("iat").GetInt64();
        long exp = doc.RootElement.GetProperty("exp").GetInt64();

        iat.Should().BeGreaterThanOrEqualTo(before);
        exp.Should().BeLessThanOrEqualTo(after);
        (exp - iat).Should().BeGreaterThan(0);
    }

    [Fact]
    public void Generate_signature_is_verifiable()
    {
        string jwt = JwtGenerator.Generate("test-client-id", _keyPath);
        string[] parts = jwt.Split('.');

        string pemText = File.ReadAllText(_keyPath);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pemText);

        byte[] dataToVerify = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");
        byte[] signature = Base64UrlDecode(parts[2]);

        bool valid = rsa.VerifyData(dataToVerify, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        valid.Should().BeTrue();
    }

    [Fact]
    public void Generate_succeeds_when_called_twice_with_same_key()
    {
        // Verifies key material cleanup doesn't corrupt state for subsequent calls
        string jwt1 = JwtGenerator.Generate("client-1", _keyPath);
        string jwt2 = JwtGenerator.Generate("client-2", _keyPath);

        jwt1.Should().NotBe(jwt2);
        jwt1.Split('.').Should().HaveCount(3);
        jwt2.Split('.').Should().HaveCount(3);
    }

    public void Dispose()
    {
        File.Delete(_keyPath);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
