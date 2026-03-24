#!/usr/bin/env bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN_DIR="${SCRIPT_DIR}/../bin"

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

# After successful push, check for an open PR if watch is requested
if [[ "${AGENT_GIT_WATCH_PR:-false}" == "true" ]]; then
    pushd "$repo_dir" > /dev/null
    pr_num=$(gh pr view --json number --jq '.number' 2>/dev/null || echo "")
    if [[ -n "$pr_num" ]]; then
        repo=$(gh repo view --json nameWithOwner --jq '.nameWithOwner' 2>/dev/null || echo "")
        if [[ -n "$repo" ]]; then
            echo "{\"event\":\"watch_request\",\"repo\":\"$repo\",\"pr\":\"$pr_num\"}"
        fi
    fi
    popd > /dev/null
fi
