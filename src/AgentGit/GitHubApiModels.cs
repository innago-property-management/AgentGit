using System.Text.Json.Serialization;

namespace AgentGit;

internal sealed class InstallationResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}

internal sealed class AccessTokenResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }
}

[JsonSerializable(typeof(InstallationResponse))]
[JsonSerializable(typeof(AccessTokenResponse))]
internal partial class GitHubApiJsonContext : JsonSerializerContext;
