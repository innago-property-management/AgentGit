namespace AgentGit;

internal interface IGitHubAppAuthenticator
{
    Task<(long InstallationId, string Token, DateTimeOffset ExpiresAt)> AuthenticateAsync(
        string jwt, string owner, string repo, CancellationToken cancellationToken = default);
}
