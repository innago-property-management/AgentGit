using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using AgentGit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Octokit;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();

builder.Services
    .AddOptions<GitHubAppSettings>()
    .BindConfiguration(GitHubAppSettings.SectionName)
    .ValidateOnStart();

using IHost host = builder.Build();

GitHubAppSettings settings = host.Services.GetRequiredService<IOptions<GitHubAppSettings>>().Value;

string jwt = GenerateGitHubJwt(settings.ClientId, settings.PrivateKeyPath);

var appClient = new GitHubClient(new ProductHeaderValue("Agent-Fleet-Manager"))
{
    Credentials = new Credentials(jwt, AuthenticationType.Bearer),
};

string repoPath = Environment.GetEnvironmentVariable("AGENT_GIT_REPO")
    ?? Directory.GetCurrentDirectory();

string rawUrl = GetRemoteUrl(repoPath);
(string owner, string repo) = ParseOwnerRepo(rawUrl);

Installation installation = await appClient.GitHubApps.GetRepositoryInstallationForCurrent(owner, repo);
AccessToken? response = await appClient.GitHubApps.CreateInstallationToken(installation.Id);
string token = response.Token;

string[] commitArgs = args.Length > 0 ? args : ["-m", "Agent update"];

var commitInfo = new ProcessStartInfo
{
    FileName = "git",
    WorkingDirectory = repoPath,
    UseShellExecute = false,
    RedirectStandardOutput = true,
    EnvironmentVariables =
    {
        ["GIT_AUTHOR_NAME"] = $"{settings.AgentName}[bot]",
        ["GIT_AUTHOR_EMAIL"] = $"{settings.AppId}+{settings.AgentName}[bot]@users.noreply.github.com",
        ["GIT_COMMITTER_NAME"] = $"{settings.AgentName}[bot]",
        ["GIT_COMMITTER_EMAIL"] = $"{settings.AppId}+{settings.AgentName}[bot]@users.noreply.github.com",
        ["GIT_ASKPASS"] = "echo",
    },
};

commitInfo.ArgumentList.Add("commit");
foreach (string arg in commitArgs)
{
    commitInfo.ArgumentList.Add(arg);
}

using (Process? commitProcess = Process.Start(commitInfo))
{
    commitProcess?.WaitForExit();
}

var pushInfo = new ProcessStartInfo
{
    FileName = "git",
    WorkingDirectory = repoPath,
    UseShellExecute = false,
    RedirectStandardOutput = true,
    EnvironmentVariables =
    {
        ["GIT_AUTHOR_NAME"] = $"{settings.AgentName}[bot]",
        ["GIT_AUTHOR_EMAIL"] = $"{settings.AppId}+{settings.AgentName}[bot]@users.noreply.github.com",
        ["GIT_COMMITTER_NAME"] = $"{settings.AgentName}[bot]",
        ["GIT_COMMITTER_EMAIL"] = $"{settings.AppId}+{settings.AgentName}[bot]@users.noreply.github.com",
        ["GIT_ASKPASS"] = "echo",
    },
};

string pushUrl = $"https://x-access-token:{token}@github.com/{owner}/{repo}.git";

pushInfo.ArgumentList.Add("push");
pushInfo.ArgumentList.Add(pushUrl);
pushInfo.ArgumentList.Add("main");

using Process? process = Process.Start(pushInfo);
process?.WaitForExit();
return;

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

    string path = parts[1].EndsWith(".git") ? parts[1][..^4] : parts[1];
    string[] segments = path.Split('/');
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
