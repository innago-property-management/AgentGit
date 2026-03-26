# AgentGit

Give AI agents their own bot identity when committing and pushing to GitHub.

AgentGit authenticates as a [GitHub App](https://docs.github.com/en/apps) using the JWT-to-installation-token flow, then commits and pushes with the app's bot identity. No more AI commits showing up as a human.

## How It Works

```
git commit -m "message"
    |
    v
AgentGit intercepts (via hook or direct invocation)
    |
    v
Generate JWT (RSA + Client ID)
    |
    v
Look up installation for target repo
    |
    v
Exchange JWT for installation token
    |
    v
git commit as YourApp[bot] with bot email
    |
    v
git push with token auth (via http.extraHeader)
```

Commits appear as your GitHub App bot — e.g., `stand-sure-ai[bot]` — with the proper `@users.noreply.github.com` email, just like other GitHub App bots (Dependabot, Renovate, etc.).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A [GitHub App](https://docs.github.com/en/apps/creating-github-apps) with:
  - **Repository permissions:** Contents (Read & Write)
  - A generated private key (`.pem` file)
  - Installed on the repositories/organizations you want to commit to
- [`gh` CLI](https://cli.github.com/) (optional, for PR watch features)

## Setup

1. **Clone and build:**

   ```bash
   git clone https://github.com/stand-sure/stand-sure-ai.git
   cd stand-sure-ai
   dotnet build AgentGit.slnx
   ```

2. **Configure your GitHub App credentials:**

   ```bash
   cp src/AgentGit/appsettings.json.example src/AgentGit/appsettings.json
   ```

   Edit `src/AgentGit/appsettings.json`:

   ```json
   {
     "GitHubApp": {
       "ClientId": "your-github-app-client-id",
       "AppId": 123456,
       "PrivateKeyPath": "/path/to/your-app.private-key.pem",
       "AgentName": "your-app-name"
     }
   }
   ```

   | Setting | Where to find it |
   |---------|-----------------|
   | `ClientId` | GitHub App settings > General > Client ID |
   | `AppId` | GitHub App settings > General > App ID |
   | `PrivateKeyPath` | Absolute path to the `.pem` you generated for the app |
   | `AgentName` | The slug name of your GitHub App (lowercase, hyphens) |

3. **Publish the release binary:**

   ```bash
   dotnet publish src/AgentGit/AgentGit.csproj -c Release -o bin
   ```

## Usage

### Via the wrapper script (recommended)

```bash
scripts/agent-git.sh /path/to/repo -m "your commit message"
scripts/agent-git.sh /path/to/repo -am "stage and commit"
scripts/agent-git.sh /path/to/repo --allow-empty -m "empty commit"
```

All arguments after the repo path pass through to `git commit` unchanged.

### Via Claude Code hook (transparent)

AgentGit was designed for [Claude Code](https://claude.ai/code). A `PreToolUse:Bash` hook can intercept `git commit` commands and rewrite them to use AgentGit. With the hook installed, agents just run `git commit` normally and get bot identity automatically.

### Environment variables

| Variable | Default | Purpose |
|----------|---------|---------|
| `AGENT_GIT_REPO` | Current directory | Target repository path |
| `AGENT_GIT_WATCH_PR` | `true` | Emit `watch_request` JSON after push if a PR exists |

## Architecture

AgentGit is a single-file .NET console app (`src/AgentGit/Program.cs`):

1. **Config** — Generic Host loads `appsettings.json` into `GitHubAppSettings` via `IOptions<T>`
2. **JWT** — RSA private key signs a JWT with `Client ID` as `iss` (pure BCL, no external JWT libraries)
3. **Installation lookup** — Parses `owner/repo` from git remote URL, calls `GetRepositoryInstallationForCurrent`
4. **Token exchange** — Swaps JWT for a scoped installation access token via Octokit
5. **Commit** — Runs `git commit` with bot identity env vars (`GIT_AUTHOR_NAME`, `GIT_COMMITTER_EMAIL`, etc.)
6. **Push** — Runs `git push` with `http.extraHeader` for token auth (never exposes token in process args)

### Bot identity format

- **Name:** `{AgentName}[bot]`
- **Email:** `{AppId}+{AgentName}[bot]@users.noreply.github.com`

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| [Octokit](https://github.com/octokit/octokit.net) | 14.0.0 | GitHub API — installation lookup and token creation |
| [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting) | — | Generic Host for configuration and DI |

## License

[MIT](LICENSE)
