# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

AgentGit is a .NET 10 console app that gives AI agents bot identity when committing and pushing to GitHub repositories. It authenticates via the `stand-sure-ai` GitHub App (public, installed on both `stand-sure` and `innago-property-management`) using JWT → installation token flow.

## Build & Publish

```bash
# Build
dotnet build AgentGit.slnx

# Publish release binary (used by the wrapper script)
dotnet publish src/AgentGit/AgentGit.csproj -c Release -o bin
```

Uses the XML-based `.slnx` solution format (not `.sln`).

## Usage

Agents don't call the binary directly. Use the wrapper script:

```bash
# Direct usage
scripts/agent-git.sh /path/to/repo -m "commit message"
scripts/agent-git.sh /path/to/repo -am "stage and commit"
scripts/agent-git.sh /path/to/repo --allow-empty -m "empty commit"
```

All args after the repo path pass through to `git commit` unchanged.

**A Claude Code hook intercepts `git commit` globally** and rewrites it to use agent-git. Agents don't need to know about this tool — plain `git commit` just works with bot identity.

## Agent Workflow Chains

### Commit → Push → Watch

```
git add <files>
git commit -m "message"          # hook rewrites to agent-git
                                  # agent-git: commit (bot identity) → push (installation token)
                                  # wrapper emits: {"event":"watch_request","repo":"owner/repo","pr":"123"}
                                  # (if a PR exists for the current branch)
```

The watch_request JSON is emitted to stdout by default. Set `AGENT_GIT_WATCH_PR=false` to suppress.

### Commit → Push → Create PR → Watch

```
git add <files>
git commit -m "message"          # agent-git handles commit + push
gh pr create --title "..." ...   # agent creates PR separately
/pr-watch <PR#>                  # agent starts monitoring CI/reviews
```

### Environment Variables

| Variable | Default | Purpose |
|----------|---------|---------|
| `AGENT_GIT_REPO` | `$(pwd)` | Target repository path |
| `AGENT_GIT_WATCH_PR` | `true` | Emit watch_request JSON after push if PR exists |

## Dependencies

- **Octokit** (v14.0.0) — GitHub API client for installation lookup and token creation
- **Microsoft.Extensions.Hosting** — Generic Host for config/DI

## Configuration

Settings live in `src/AgentGit/appsettings.json`:

| Setting | Purpose |
|---------|---------|
| `GitHubApp:ClientId` | GitHub App Client ID (JWT issuer) |
| `GitHubApp:AppId` | GitHub App ID (for bot email format) |
| `GitHubApp:PrivateKeyPath` | Path to `.pem` private key file |
| `GitHubApp:AgentName` | Bot name (used in commit identity) |

Installation ID is resolved dynamically from the target repo's remote — no hardcoding needed.

## Architecture

`src/AgentGit/Program.cs` — single-file top-level program:

1. Load config via Generic Host + `IOptions<GitHubAppSettings>`
2. Generate JWT from private key using BCL crypto (`System.Security.Cryptography`) with Client ID as `iss`
3. Parse `owner/repo` from git remote URL (handles both HTTPS and SSH)
4. Look up installation dynamically via `GetRepositoryInstallationForCurrent(owner, repo)`
5. Exchange JWT for installation access token via Octokit
6. `git commit` with `ArgumentList` (preserves quoting) and bot identity env vars
7. `git push` to authenticated URL with current branch detection

Bot identity format: `{agentName}[bot]` / `{appId}+{agentName}[bot]@users.noreply.github.com`

## Hook Integration

A global Claude Code `PreToolUse:Bash` hook (`~/.claude/hooks/agent-git-intercept.sh`) intercepts any Bash command containing `git commit` and rewrites it to use agent-git. Handles:

- Standalone: `git commit -m "msg"`
- Chained: `git add . && git commit -m "msg"`
- Any git commit flags pass through unchanged
