using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AgentGit;

internal sealed class GitHubAppAuthenticator(HttpClient httpClient) : IGitHubAppAuthenticator
{
    public async Task<(long InstallationId, string Token, DateTimeOffset ExpiresAt)> AuthenticateAsync(
        string jwt, string owner, string repo, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.github.com/repos/{owner}/{repo}/installation");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("AgentGit", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var installResponse = await httpClient.SendAsync(request, cancellationToken);
        installResponse.EnsureSuccessStatusCode();

        var installation = await installResponse.Content.ReadFromJsonAsync(
            GitHubApiJsonContext.Default.InstallationResponse,
            cancellationToken)
            ?? throw new InvalidOperationException($"No installation found for {owner}/{repo}");

        // Step 2: Create installation access token
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post,
            $"https://api.github.com/app/installations/{installation.Id}/access_tokens");
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        tokenRequest.Headers.UserAgent.Add(new ProductInfoHeaderValue("AgentGit", "1.0"));
        tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var tokenResponse = await httpClient.SendAsync(tokenRequest, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        var accessToken = await tokenResponse.Content.ReadFromJsonAsync(
            GitHubApiJsonContext.Default.AccessTokenResponse,
            cancellationToken)
            ?? throw new InvalidOperationException("Failed to parse access token response");

        return (installation.Id, accessToken.Token, accessToken.ExpiresAt);
    }
}
