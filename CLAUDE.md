# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

AgentGit is a .NET 10 Native AOT console app that gives AI agents bot identity when committing and pushing to GitHub repositories. It authenticates via the `stand-sure-ai` GitHub App (public, installed on both `stand-sure` and `innago-property-management`) using JWT → installation token flow.

## Build & Publish

```bash
# Build
dotnet build AgentGit.slnx

# Run tests
dotnet test AgentGit.slnx

# Publish Native AOT binary (used by the wrapper script)
dotnet publish src/AgentGit/AgentGit.csproj -c Release -o bin
```

Uses the XML-based `.slnx` solution format (not `.sln`). Produces an ~8MB native arm64 binary.

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

### Commit → Push → Auto-PR (Innago repos)

Innago repos with `.github/workflows/auto-pr.yml` automatically create a PR when a branch is pushed. No `gh pr create` needed — just push and the watch_request will fire on the next commit once the PR exists.

### Commit → Push → Create PR → Watch

For repos without auto-PR (like `stand-sure` personal repos):

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

- **Microsoft.Extensions.Hosting** — Generic Host for config/DI
- **Microsoft.Extensions.Http** — HttpClient factory for GitHub API calls

No Octokit. All JSON uses source-generated `JsonSerializerContext` for AOT compatibility.

## Configuration

Settings via `src/AgentGit/appsettings.json` or environment variables (`GitHubApp__*`):

| Setting | Env Var | Purpose |
|---------|---------|---------|
| `GitHubApp:ClientId` | `GitHubApp__ClientId` | GitHub App Client ID (JWT issuer) |
| `GitHubApp:AppId` | `GitHubApp__AppId` | GitHub App ID (for bot email format) |
| `GitHubApp:PrivateKeyPath` | `GitHubApp__PrivateKeyPath` | Path to `.pem` private key file |
| `GitHubApp:AgentName` | `GitHubApp__AgentName` | Bot name (used in commit identity) |

`appsettings.json` is gitignored. Use `fnox exec` or env vars for secret management. Installation ID is resolved dynamically from the target repo's remote.

## Architecture

Native AOT binary with single-responsibility internal classes:

| Class | Responsibility |
|-------|---------------|
| `Program.cs` | DI wiring + orchestration (thin) |
| `JwtGenerator` | RSA JWT creation with source-gen JSON |
| `RemoteUrlParser` | Parse owner/repo from git remote URL |
| `GitHubAppAuthenticator` / `IGitHubAppAuthenticator` | Installation lookup + token exchange via HttpClient |
| `GitProcessRunner` / `IGitProcessRunner` | Git commit, push, branch, remote operations |
| `PrivateKeyValidator` | File existence + Unix permission checks |
| `AskPassScriptManager` | Cross-platform askpass script lifecycle |
| `LoggerMessages` | LoggerMessage source generator for structured logging |
| `GitHubAppSettings` | IOptions<T> configuration model |

**Flow:**
1. Load config via Generic Host + `IOptions<GitHubAppSettings>`
2. Validate private key permissions (`PrivateKeyValidator`)
3. Parse `owner/repo` from git remote (`RemoteUrlParser`)
4. Generate JWT (`JwtGenerator`) and exchange for installation token (`GitHubAppAuthenticator`)
5. `git commit` with bot identity env vars (`GitProcessRunner`)
6. `git push` via `GIT_ASKPASS` — token never in CLI args or `ps aux` (`GitProcessRunner`)

Bot identity format: `{agentName}[bot]` / `{appId}+{agentName}[bot]@users.noreply.github.com`

## Hook Integration

A global Claude Code `PreToolUse:Bash` hook (`~/.claude/hooks/agent-git-intercept.sh`) intercepts any Bash command containing `git commit` and rewrites it to use agent-git. Handles:

- Standalone: `git commit -m "msg"`
- Chained: `git add . && git commit -m "msg"`
- Any git commit flags pass through unchanged
