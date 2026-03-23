using System.Diagnostics;

using GitHubJwt;

using Octokit;

int appId = int.Parse(Environment.GetEnvironmentVariable("GH_APP_ID") ?? "3167794");
long installId = long.Parse(Environment.GetEnvironmentVariable("GH_INSTALL_ID") ?? "118505573");
string privateKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "/Users/christopheranderson/Downloads/stand-sure-ai.2026-03-23.private-key.pem");

var privateKeySource = new FilePrivateKeySource(privateKeyPath);

var generator = new GitHubJwtFactory(privateKeySource,
    new GitHubJwtFactoryOptions { AppIntegrationId = appId, ExpirationSeconds = TimeSpan.FromMinutes(10).Seconds });

string? jwt = generator.CreateEncodedJwtToken();

var appClient = new GitHubClient(new ProductHeaderValue("Agent-Fleet-Manager"))
{
    Credentials = new Credentials(jwt, AuthenticationType.Bearer),
};

AccessToken? response = await appClient.GitHubApps.CreateInstallationToken(installId);
string token = response.Token;

const string agentName = "stand-sure-ai";
string repoPath = Directory.GetCurrentDirectory();

string commitMessage = args[1];

commitMessage = string.IsNullOrWhiteSpace(commitMessage) ? "Agent update" : commitMessage;

var commitInfo = new ProcessStartInfo
{
    FileName = "git",
    Arguments = $"commit -m \"{commitMessage}\"",
    WorkingDirectory = repoPath,
    UseShellExecute = false,
    RedirectStandardOutput = true,
    EnvironmentVariables =
    {
        ["GIT_AUTHOR_NAME"] = $"{agentName}[bot]",
        ["GIT_AUTHOR_EMAIL"] = $"{appId}+{agentName}[bot]@users.noreply.github.com",
        ["GIT_COMMITTER_NAME"] = $"{agentName}[bot]",
        ["GIT_COMMITTER_EMAIL"] = $"{appId}+{agentName}[bot]@users.noreply.github.com",
        ["GIT_ASKPASS"] = "echo",
    },
};

using (Process? commitProcess = Process.Start(commitInfo))
{
    commitProcess?.WaitForExit();
}

var pushInfo = new ProcessStartInfo
{
    FileName = "git",
    Arguments = "push origin main",
    WorkingDirectory = repoPath,
    UseShellExecute = false,
    RedirectStandardOutput = true,
    EnvironmentVariables =
    {
        ["GIT_AUTHOR_NAME"] = $"{agentName}[bot]",
        ["GIT_AUTHOR_EMAIL"] = $"{appId}+{agentName}[bot]@users.noreply.github.com",
        ["GIT_COMMITTER_NAME"] = $"{agentName}[bot]",
        ["GIT_COMMITTER_EMAIL"] = $"{appId}+{agentName}[bot]@users.noreply.github.com",
        ["GIT_ASKPASS"] = "echo",
    },
};

string rawUrl = GetRemoteUrl(repoPath);

string pushUrl = rawUrl;

if (rawUrl.Contains("github.com"))
{
    string[] parts = rawUrl.Split("github.com/");

    if (parts.Length < 2)
    {
        parts = rawUrl.Split("github.com:");
    }

    string repoPathPart = parts[1];
    pushUrl = $"https://x-access-token:{token}@github.com/{repoPathPart}";
}

pushInfo.Arguments = $"push {pushUrl} main";

using Process? process = Process.Start(pushInfo);
process?.WaitForExit();
return;

static string GetRemoteUrl(string repoPath)
{
    var psi = new ProcessStartInfo("git", "remote get-url origin")
    {
        WorkingDirectory = repoPath,
        RedirectStandardOutput = true,
        UseShellExecute = false,
    };

    using Process? process = Process.Start(psi);
    return process?.StandardOutput.ReadToEnd().Trim() ?? "";
}