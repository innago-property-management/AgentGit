namespace AgentGit;

public sealed class GitHubAppSettings
{
    public const string SectionName = "GitHubApp";

    public string ClientId { get; set; } = "";
    public int AppId { get; set; }
    public string PrivateKeyPath { get; set; } = "";
    public string AgentName { get; set; } = "";
}
