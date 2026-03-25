#!/usr/bin/env bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="${SCRIPT_DIR}/../src/AgentGit"
BIN_DIR="${SCRIPT_DIR}/../bin"

# Rebuild if source is newer than published binary
if [[ "${PROJECT_DIR}/Program.cs" -nt "${BIN_DIR}/AgentGit" ]] || \
   [[ "${PROJECT_DIR}/appsettings.json" -nt "${BIN_DIR}/appsettings.json" ]] || \
   [[ "${PROJECT_DIR}/AgentGit.csproj" -nt "${BIN_DIR}/AgentGit" ]]; then
    dotnet publish "${PROJECT_DIR}/AgentGit.csproj" -c Release -o "${BIN_DIR}" --nologo -v quiet 2>&1
fi

repo_dir="$1"
shift

if [[ -z "$repo_dir" ]]; then
    echo "Usage: agent-git.sh <repo-directory> [git commit args...]"
    echo "Example: agent-git.sh /path/to/repo -m \"my commit message\""
    exit 1
fi

if [[ ! -d "$repo_dir/.git" ]]; then
    echo "Error: Not a git repository: $repo_dir"
    exit 1
fi

export AGENT_GIT_REPO="$repo_dir"

pushd "${BIN_DIR}" > /dev/null
./AgentGit "$@"
popd > /dev/null

# After successful push, check for an open PR (default: on, set AGENT_GIT_WATCH_PR=false to skip)
if [[ "${AGENT_GIT_WATCH_PR:-true}" != "false" ]]; then
    pushd "$repo_dir" > /dev/null
    pr_num=$(gh pr view --json number --jq '.number' 2>/dev/null || echo "")
    if [[ -n "$pr_num" ]]; then
        repo=$(gh repo view --json nameWithOwner --jq '.nameWithOwner' 2>/dev/null || echo "")
        if [[ -n "$repo" ]]; then
            cat <<WATCH_EOF
{"event":"watch_request","repo":"$repo","pr":"$pr_num","action":"To monitor this PR for CI and reviews, call mcp-langchain-bridge execute_tool with toolName='watch', serverName='pr-watch', arguments={'repo':'$repo','pr':'$pr_num'}"}
WATCH_EOF
        fi
    fi
    popd > /dev/null
fi
