namespace AgentGit;

public sealed class GitHubAppSettings
{
    public const string SectionName = "GitHubApp";

    public required string ClientId { get; init; }
    public required int AppId { get; init; }
    public required string PrivateKeyPath { get; init; }
    public required string AgentName { get; init; }
}
