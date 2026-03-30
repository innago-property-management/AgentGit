namespace AgentGit;

internal static class RemoteUrlParser
{
    internal static (string Owner, string Repo) Parse(string remoteUrl)
    {
        string[] parts = remoteUrl.Split("github.com/");

        if (parts.Length < 2)
        {
            parts = remoteUrl.Split("github.com:");
        }

        if (parts.Length < 2)
        {
            return ("", "");
        }

        string path = parts[1].EndsWith(".git") ? parts[1][..^4] : parts[1];
        string[] segments = path.Split('/');

        if (segments.Length < 2)
        {
            return ("", "");
        }

        return (segments[0], segments[1]);
    }
}
