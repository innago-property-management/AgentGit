using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using AgentGit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Octokit;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();

builder.Services
    .AddOptions<GitHubAppSettings>()
    .BindConfiguration(GitHubAppSettings.SectionName)
    .ValidateOnStart();

using IHost host = builder.Build();

ILogger logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AgentGit");

GitHubAppSettings settings = host.Services.GetRequiredService<IOptions<GitHubAppSettings>>().Value;

string repoPath = Environment.GetEnvironmentVariable("AGENT_GIT_REPO")
    ?? Directory.GetCurrentDirectory();

if (!File.Exists(settings.PrivateKeyPath))
{
    logger.PrivateKeyNotFound(settings.PrivateKeyPath);
    return 2;
}

if (!OperatingSystem.IsWindows())
{
    UnixFileMode mode = File.GetUnixFileMode(settings.PrivateKeyPath);
    if ((mode & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead | UnixFileMode.OtherWrite)) != 0)
    {
        logger.LogError("Private key {Path} has overly permissive file mode {Mode}. Run: chmod 600 {Path}",
            settings.PrivateKeyPath, mode, settings.PrivateKeyPath);
        return 2;
    }
}

logger.GeneratingJwt(settings.ClientId);
string jwt = GenerateGitHubJwt(settings.ClientId, settings.PrivateKeyPath);

var appClient = new GitHubClient(new ProductHeaderValue("Agent-Fleet-Manager"))
{
    Credentials = new Credentials(jwt, AuthenticationType.Bearer),
};

string rawUrl = GetRemoteUrl(repoPath);
(string owner, string repo) = ParseOwnerRepo(rawUrl);

if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
{
    logger.RemoteParseFailed(rawUrl);
    return 3;
}

logger.ResolvedRepo(owner, repo);

Installation installation = await appClient.GitHubApps.GetRepositoryInstallationForCurrent(owner, repo);
logger.FoundInstallation(installation.Id, owner, repo);

AccessToken? response = await appClient.GitHubApps.CreateInstallationToken(installation.Id);
string token = response.Token;
logger.TokenAcquired(response.ExpiresAt);

string[] commitArgs = args.Length > 0 ? args : ["-m", "Agent update"];

string botName = $"{settings.AgentName}[bot]";
string botEmail = $"{settings.AppId}+{settings.AgentName}[bot]@users.noreply.github.com";

var commitInfo = new ProcessStartInfo
{
    FileName = "git",
    WorkingDirectory = repoPath,
    UseShellExecute = false,
    RedirectStandardOutput = true,
    EnvironmentVariables =
    {
        ["GIT_AUTHOR_NAME"] = botName,
        ["GIT_AUTHOR_EMAIL"] = botEmail,
        ["GIT_COMMITTER_NAME"] = botName,
        ["GIT_COMMITTER_EMAIL"] = botEmail,
        ["GIT_ASKPASS"] = "echo",
    },
};

commitInfo.ArgumentList.Add("commit");
commitInfo.ArgumentList.Add("--no-gpg-sign");
foreach (string arg in commitArgs)
{
    commitInfo.ArgumentList.Add(arg);
}

logger.Committing(botName, string.Join(" ", commitArgs));

using (Process? commitProcess = Process.Start(commitInfo))
{
    commitProcess?.WaitForExit();
    if (commitProcess is { ExitCode: not 0 })
    {
        logger.CommitFailed(commitProcess.ExitCode);
        return commitProcess.ExitCode;
    }
}

string currentBranch = GetCurrentBranch(repoPath);

var pushInfo = new ProcessStartInfo
{
    FileName = "git",
    WorkingDirectory = repoPath,
    UseShellExecute = false,
    RedirectStandardOutput = true,
    EnvironmentVariables =
    {
        ["GIT_AUTHOR_NAME"] = botName,
        ["GIT_AUTHOR_EMAIL"] = botEmail,
        ["GIT_COMMITTER_NAME"] = botName,
        ["GIT_COMMITTER_EMAIL"] = botEmail,
        ["GIT_ASKPASS"] = "echo",
    },
};

logger.Pushing(owner, repo, currentBranch);

// Token passed via env var + GIT_ASKPASS — keeps it out of ps aux (CLI args are world-readable)
string askPassScript = Path.Combine(Path.GetTempPath(), $"agentgit-askpass-{Environment.ProcessId}.sh");
File.WriteAllText(askPassScript, "#!/bin/sh\necho \"$AGENTGIT_TOKEN\"\n");
if (!OperatingSystem.IsWindows())
{
    File.SetUnixFileMode(askPassScript, UnixFileMode.UserRead | UnixFileMode.UserExecute);
}

pushInfo.EnvironmentVariables["AGENTGIT_TOKEN"] = token;
pushInfo.EnvironmentVariables["GIT_ASKPASS"] = askPassScript;
pushInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";

// Disable credential helpers so they don't override GIT_ASKPASS (e.g., macOS keychain)
string pushUrlWithUser = $"https://x-access-token@github.com/{owner}/{repo}.git";
pushInfo.ArgumentList.Add("-c");
pushInfo.ArgumentList.Add("credential.helper=");
pushInfo.ArgumentList.Add("push");
pushInfo.ArgumentList.Add(pushUrlWithUser);
pushInfo.ArgumentList.Add(currentBranch);

using Process? pushProcess = Process.Start(pushInfo);
pushProcess?.WaitForExit();

try
{
    File.Delete(askPassScript);
}
catch
{
    // Best-effort cleanup
}

if (pushProcess is { ExitCode: not 0 })
{
    logger.PushFailed(pushProcess.ExitCode);
    return pushProcess.ExitCode;
}

return 0;

static string GetCurrentBranch(string repoPath)
{
    var psi = new ProcessStartInfo("git")
    {
        WorkingDirectory = repoPath,
        RedirectStandardOutput = true,
        UseShellExecute = false,
    };

    psi.ArgumentList.Add("rev-parse");
    psi.ArgumentList.Add("--abbrev-ref");
    psi.ArgumentList.Add("HEAD");

    using Process? process = Process.Start(psi);
    return process?.StandardOutput.ReadToEnd().Trim() ?? "main";
}

static string GetRemoteUrl(string repoPath)
{
    var psi = new ProcessStartInfo("git")
    {
        WorkingDirectory = repoPath,
        RedirectStandardOutput = true,
        UseShellExecute = false,
    };

    psi.ArgumentList.Add("remote");
    psi.ArgumentList.Add("get-url");
    psi.ArgumentList.Add("origin");

    using Process? process = Process.Start(psi);
    return process?.StandardOutput.ReadToEnd().Trim() ?? "";
}

static (string Owner, string Repo) ParseOwnerRepo(string remoteUrl)
{
    // Handles both HTTPS (github.com/) and SSH (github.com:) formats
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

static string GenerateGitHubJwt(string clientId, string privateKeyPath)
{
    string pemText = File.ReadAllText(privateKeyPath);
    using var rsa = RSA.Create();
    rsa.ImportFromPem(pemText);

    long iat = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeSeconds();
    long exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();

    string header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
        new { alg = "RS256", typ = "JWT" }));

    string payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
        new { iat, exp, iss = clientId }));

    byte[] signature = rsa.SignData(
        Encoding.UTF8.GetBytes($"{header}.{payload}"),
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);

    return $"{header}.{payload}.{Base64UrlEncode(signature)}";
}

static string Base64UrlEncode(byte[] data)
{
    return Convert.ToBase64String(data)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}
