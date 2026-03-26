using Microsoft.Extensions.Logging;

namespace AgentGit;

internal static partial class LoggerMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Generating JWT for client {ClientId}")]
    public static partial void GeneratingJwt(this ILogger logger, string clientId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Resolved repository: {Owner}/{Repo}")]
    public static partial void ResolvedRepo(this ILogger logger, string owner, string repo);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found installation {InstallationId} for {Owner}/{Repo}")]
    public static partial void FoundInstallation(this ILogger logger, long installationId, string owner, string repo);

    [LoggerMessage(Level = LogLevel.Information, Message = "Installation token acquired, expires at {ExpiresAt}")]
    public static partial void TokenAcquired(this ILogger logger, DateTimeOffset expiresAt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Committing as {BotName} with args: {Args}")]
    public static partial void Committing(this ILogger logger, string botName, string args);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pushing to {Owner}/{Repo} branch {Branch}")]
    public static partial void Pushing(this ILogger logger, string owner, string repo, string branch);

    [LoggerMessage(Level = LogLevel.Error, Message = "git commit failed with exit code {ExitCode}")]
    public static partial void CommitFailed(this ILogger logger, int exitCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "git push failed with exit code {ExitCode}")]
    public static partial void PushFailed(this ILogger logger, int exitCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse remote URL: {RemoteUrl}")]
    public static partial void RemoteParseFailed(this ILogger logger, string remoteUrl);

    [LoggerMessage(Level = LogLevel.Error, Message = "Private key file not found: {Path}")]
    public static partial void PrivateKeyNotFound(this ILogger logger, string path);
}
