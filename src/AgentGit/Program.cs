using AgentGit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();

builder.Services
    .AddOptions<GitHubAppSettings>()
    .BindConfiguration(GitHubAppSettings.SectionName)
    .ValidateOnStart();

builder.Services.AddHttpClient<IGitHubAppAuthenticator, GitHubAppAuthenticator>();
builder.Services.AddSingleton<IGitProcessRunner, GitProcessRunner>();

using IHost host = builder.Build();

ILogger logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AgentGit");
GitHubAppSettings settings = host.Services.GetRequiredService<IOptions<GitHubAppSettings>>().Value;
IGitProcessRunner git = host.Services.GetRequiredService<IGitProcessRunner>();
IGitHubAppAuthenticator auth = host.Services.GetRequiredService<IGitHubAppAuthenticator>();

string repoPath = Environment.GetEnvironmentVariable("AGENT_GIT_REPO")
    ?? Directory.GetCurrentDirectory();

// Validate private key
var keyValidation = PrivateKeyValidator.Validate(settings.PrivateKeyPath);
if (!keyValidation.IsValid)
{
    logger.LogError("{Error}", keyValidation.Error);
    return 2;
}

// Parse remote
string rawUrl = git.GetRemoteUrl(repoPath);
var (owner, repo) = RemoteUrlParser.Parse(rawUrl);
if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
{
    logger.RemoteParseFailed(rawUrl);
    return 3;
}

logger.ResolvedRepo(owner, repo);

// Authenticate
logger.GeneratingJwt(settings.ClientId);
string jwt = JwtGenerator.Generate(settings.ClientId, settings.PrivateKeyPath);

var (installationId, token, expiresAt) = await auth.AuthenticateAsync(jwt, owner, repo);
logger.FoundInstallation(installationId, owner, repo);
logger.TokenAcquired(expiresAt);

// Commit
string[] commitArgs = args.Length > 0 ? args : ["-m", "Agent update"];
string botName = $"{settings.AgentName}[bot]";
string botEmail = $"{settings.AppId}+{settings.AgentName}[bot]@users.noreply.github.com";

logger.Committing(botName, string.Join(" ", commitArgs));
int commitExit = git.Commit(repoPath, botName, botEmail, commitArgs);
if (commitExit != 0)
{
    logger.CommitFailed(commitExit);
    return commitExit;
}

// Push
string currentBranch = git.GetCurrentBranch(repoPath);
logger.Pushing(owner, repo, currentBranch);
int pushExit = git.Push(repoPath, botName, botEmail, token, owner, repo, currentBranch);
if (pushExit != 0)
{
    logger.PushFailed(pushExit);
    return pushExit;
}

return 0;
