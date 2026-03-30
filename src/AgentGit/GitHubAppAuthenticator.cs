using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgentGit;

internal sealed class GitHubAppAuthenticator(HttpClient httpClient) : IGitHubAppAuthenticator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = GitHubApiJsonContext.Default,
    };

    public async Task<(long InstallationId, string Token, DateTimeOffset ExpiresAt)> AuthenticateAsync(
        string jwt, string owner, string repo, CancellationToken cancellationToken = default)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AgentGit", "1.0"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        // Step 1: Get installation for repo
        var installation = await httpClient.GetFromJsonAsync(
            $"https://api.github.com/repos/{owner}/{repo}/installation",
            GitHubApiJsonContext.Default.InstallationResponse,
            cancellationToken)
            ?? throw new InvalidOperationException($"No installation found for {owner}/{repo}");

        // Step 2: Create installation access token
        var response = await httpClient.PostAsync(
            $"https://api.github.com/app/installations/{installation.Id}/access_tokens",
            null,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync(
            GitHubApiJsonContext.Default.AccessTokenResponse,
            cancellationToken)
            ?? throw new InvalidOperationException("Failed to parse access token response");

        return (installation.Id, tokenResponse.Token, tokenResponse.ExpiresAt);
    }
}
