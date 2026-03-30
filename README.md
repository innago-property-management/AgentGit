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
git push with token auth (via GIT_ASKPASS)
```

Commits appear as your GitHub App bot — e.g., `stand-sure-ai[bot]` — with the proper `@users.noreply.github.com` email, just like other GitHub App bots (Dependabot, Renovate, etc.).

## Creating Your GitHub App

You need a GitHub App to give your agent its own identity. This takes about 5 minutes.

### Step 1: Register the App

1. Go to **GitHub Settings** > **Developer settings** > **GitHub Apps** > **New GitHub App**
   - Direct link: https://github.com/settings/apps/new
2. Fill in:
   - **GitHub App name**: Something like `my-agent` or `my-org-ai-bot` (this becomes the `[bot]` name in commits)
   - **Homepage URL**: Your repo URL or any URL
   - **Webhook**: Uncheck "Active" (AgentGit doesn't need webhooks)

### Step 2: Set Permissions

Under **Repository permissions**, set:

| Permission | Access | Why |
|-----------|--------|-----|
| **Contents** | Read & Write | Commit and push to repositories |

That's it — no other permissions needed.

### Step 3: Choose Installation Scope

Under **Where can this GitHub App be installed?**:

- **Only on this account** — if you only need it for your personal repos
- **Any account** — if you want to install it on organizations too

Click **Create GitHub App**.

### Step 4: Note Your App Credentials

After creation, you'll be on the app's settings page. Note these values:

| Setting | Where to find it | Example |
|---------|-----------------|---------|
| **App ID** | General > About > App ID | `3167794` |
| **Client ID** | General > About > Client ID | `Iv23li8yGhLhlDV5cm0A` |
| **App name** | The slug in the URL (`github.com/settings/apps/<name>`) | `my-agent` |

### Step 5: Generate a Private Key

1. On the app settings page, scroll to **Private keys**
2. Click **Generate a private key**
3. A `.pem` file downloads — save it somewhere secure
4. Set permissions: `chmod 600 /path/to/your-key.pem` (AgentGit enforces this)

### Step 6: Install the App on Your Repositories

1. In the app settings, click **Install App** in the left sidebar
2. Choose the account/organization
3. Select **All repositories** or **Only select repositories**
4. Click **Install**

Repeat for each organization you want the agent to commit to. AgentGit dynamically looks up the installation for each repo, so a single app can work across multiple orgs.

## Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (for building from source)
- A GitHub App (see above)
- [`gh` CLI](https://cli.github.com/) (optional, for PR watch features)

### Build

```bash
git clone https://github.com/innago-property-management/AgentGit.git
cd AgentGit
dotnet publish src/AgentGit/AgentGit.csproj -c Release -o bin
```

This produces a native AOT binary (~8MB, no .NET runtime required at runtime).

### Configure

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

Alternatively, set environment variables (useful for CI or secret managers):

```bash
export GitHubApp__ClientId="your-client-id"
export GitHubApp__AppId="123456"
export GitHubApp__PrivateKeyPath="/path/to/key.pem"
export GitHubApp__AgentName="your-app-name"
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

AgentGit is a .NET 10 console app published as a Native AOT binary:

1. **Config** — Generic Host loads `appsettings.json` (or env vars) into `GitHubAppSettings` via `IOptions<T>`
2. **JWT** — `JwtGenerator` signs a JWT with RSA private key and Client ID as `iss` (pure BCL crypto, source-generated JSON)
3. **Installation lookup** — `RemoteUrlParser` extracts `owner/repo` from git remote, `GitHubAppAuthenticator` calls the GitHub API
4. **Token exchange** — `GitHubAppAuthenticator` swaps JWT for a scoped installation access token via direct HTTP (no Octokit)
5. **Commit** — `GitProcessRunner` runs `git commit` with bot identity env vars (`GIT_AUTHOR_NAME`, `GIT_COMMITTER_EMAIL`, etc.)
6. **Push** — `GitProcessRunner` runs `git push` with `GIT_ASKPASS` for token auth (token never visible in `ps aux`)

### Security

- Private key permissions enforced (`PrivateKeyValidator` rejects world-readable keys)
- Token passed via `GIT_ASKPASS` + env var, never in CLI args or URLs
- Credential helpers disabled during push to prevent keychain override
- `AskPassScriptManager` creates temporary scripts with restrictive permissions, cleaned up after use

### Bot identity format

- **Name:** `{AgentName}[bot]`
- **Email:** `{AppId}+{AgentName}[bot]@users.noreply.github.com`

## Dependencies

| Package | Purpose |
|---------|---------|
| [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting) | Generic Host for configuration and DI |
| [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http) | HttpClient factory for GitHub API calls |

Zero reflection. All JSON serialization uses compile-time source generators for Native AOT compatibility.

## License

[MIT](LICENSE)
