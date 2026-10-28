#!/usr/bin/env bash
# Scans the repository's git history for secrets with gitleaks, or what the given flags select, redacting what it
# finds. The pre-commit hook and the CI history scan both call it, so the gitleaks version lives here only. Runs a local
# `gitleaks` of that version, else the pinned image through Docker, else a local `gitleaks` of another version. The
# container sees the repository and git's common directory at their host paths and inherits GIT_INDEX_FILE, so worktrees
# and `git commit -a` scan what will be committed.
# Exits 2 when it finds a secret, 3 when neither gitleaks nor Docker is available, and gitleaks' or Docker's own code
# when the scan fails.
# Usage: scripts/gitleaks.sh [gitleaks git flags]
set -euo pipefail

version=8.30.1

cd "$(dirname "$0")/.."

scan=(git --no-banner --redact --verbose --exit-code 2 "$@")

installed=""
if command -v gitleaks >/dev/null; then
    installed=$(gitleaks version)
    if [[ "${installed#v}" == "$version" ]]; then
        exec gitleaks "${scan[@]}" .
    fi
fi

if command -v docker >/dev/null && docker info >/dev/null 2>&1; then
    common=$(git rev-parse --path-format=absolute --git-common-dir)
    exec docker run --rm -v "$PWD:$PWD" -v "$common:$common" -w "$PWD" -e GIT_INDEX_FILE \
        "ghcr.io/gitleaks/gitleaks:v$version" "${scan[@]}" "$PWD"
fi

if [[ -n "$installed" ]]; then
    echo "gitleaks $installed is installed, but Flownodes pins $version." >&2
    exec gitleaks "${scan[@]}" .
fi

echo "Neither gitleaks nor Docker is available." >&2
exit 3
