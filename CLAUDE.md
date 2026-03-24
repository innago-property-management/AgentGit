# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

AgentGit is a .NET 10 console app that enables a GitHub App (`stand-sure-ai`) to commit and push to repositories using bot credentials. It authenticates via GitHub App JWT → installation token flow, then shells out to `git commit` and `git push` with bot identity environment variables.

## Build & Run

```bash
# Build
dotnet build AgentGit.slnx

# Run (requires a commit message argument)
dotnet run --project src/AgentGit -- "commit message here"
```

Uses the XML-based `.slnx` solution format (not `.sln`).

## Dependencies

- **Octokit** (v14.0.0) — GitHub API client for installation token creation
- **GitHubJwt** (v0.0.6) — JWT generation for GitHub App authentication

## Configuration

The app reads from environment variables with hardcoded fallbacks:
- `GH_APP_ID` — GitHub App ID (default: `3167794`)
- `GH_INSTALL_ID` — GitHub Installation ID (default: `118505573`)
- Private key path is currently hardcoded to a local file path in Program.cs

## Architecture

Single-file top-level program (`src/AgentGit/Program.cs`) with this flow:
1. Generate JWT from GitHub App private key
2. Exchange JWT for installation access token via Octokit
3. `git commit` with bot identity env vars (`GIT_AUTHOR_NAME`, `GIT_COMMITTER_NAME`, etc.)
4. Rewrite remote URL to inject `x-access-token:{token}` for authenticated push
5. `git push` to origin main

The bot identity format follows GitHub convention: `{agentName}[bot]` / `{appId}+{agentName}[bot]@users.noreply.github.com`
