#!/usr/bin/env bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="${SCRIPT_DIR}/../src/AgentGit"

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

pushd "$PROJECT_DIR" > /dev/null
dotnet run -- "$@"
popd > /dev/null
