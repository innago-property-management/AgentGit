using System.Diagnostics;

using GitHubJwt; // NuGet: GitHubJwt
using Octokit; // NuGet: Octokit

// 1. Setup App Credentials
int appId = int.Parse(Environment.GetEnvironmentVariable("GH_APP_ID") ?? "3167794");
long installId = long.Parse(Environment.GetEnvironmentVariable("GH_INSTALL_ID") ?? "118505573");
string privateKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "/Users/christopheranderson/Downloads/stand-sure-ai.2026-03-23.private-key.pem");

// 2. Generate JWT and Exchange for Installation Token
var privateKeySource = new FilePrivateKeySource(privateKeyPath);

var generator = new GitHubJwtFactory(privateKeySource,
    new GitHubJwtFactoryOptions { AppIntegrationId = appId, ExpirationSeconds = TimeSpan.FromMinutes(10).Seconds });

string? jwt = generator.CreateEncodedJwtToken();

var appClient = new GitHubClient(new ProductHeaderValue("Agent-Fleet-Manager"))
{
    Credentials = new Credentials(jwt, AuthenticationType.Bearer)
};

AccessToken? response = await appClient.GitHubApps.CreateInstallationToken(installId);
string token = response.Token;

// 3. Execute Git with Injected Identity
const string agentName = "stand-sure-ai";
string repoPath = Directory.GetCurrentDirectory();

var startInfo = new ProcessStartInfo
{
    FileName = "git",
    Arguments = "push origin main",
    WorkingDirectory = repoPath,
    UseShellExecute = false,
    RedirectStandardOutput = true
};

// These ENV vars override your global ~/.gitconfig for THIS process only
startInfo.EnvironmentVariables["GIT_AUTHOR_NAME"] = $"{agentName}[bot]";
startInfo.EnvironmentVariables["GIT_AUTHOR_EMAIL"] = $"{appId}+{agentName}[bot]@users.noreply.github.com";
startInfo.EnvironmentVariables["GIT_COMMITTER_NAME"] = $"{agentName}[bot]";
startInfo.EnvironmentVariables["GIT_COMMITTER_EMAIL"] = $"{appId}+{agentName}[bot]@users.noreply.github.com";

// Use the token for HTTPS Auth without storing it
startInfo.EnvironmentVariables["GIT_ASKPASS"] = "echo"; // Disable prompt

var rawUrl = GetRemoteUrl(repoPath);

// Standardize the URL for Token Auth
// This regex handles both:
// 1. https://github.com/owner/repo.git
// 2. git@github.com:owner/repo.git
string pushUrl = rawUrl;

if (rawUrl.Contains("github.com"))
{
    // Strip the protocol/user and rebuild with the token
    string[] parts = rawUrl.Split("github.com/"); // For HTTPS

    if (parts.Length < 2)
    {
        parts = rawUrl.Split("github.com:"); // For SSH
    }

    var repoPathPart = parts[1];
    pushUrl = $"https://x-access-token:{token}@github.com/{repoPathPart}";
}

// 3. The Final Push Command
startInfo.Arguments = $"push {pushUrl} main";

using Process? process = Process.Start(startInfo);
process?.WaitForExit();

// Helper to get the existing remote URL
static string GetRemoteUrl(string repoPath)
{
    var psi = new ProcessStartInfo("git", "remote get-url origin")
    {
        WorkingDirectory = repoPath,
        RedirectStandardOutput = true,
        UseShellExecute = false
    };

    using var process = Process.Start(psi);
    return process?.StandardOutput.ReadToEnd().Trim() ?? "";
}