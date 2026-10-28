#!/usr/bin/env bash
# Scans the repository's git history for secrets with gitleaks, redacting what it finds. The pre-commit hook and the CI
# history scan both call it, so the gitleaks version lives here only. Runs a local `gitleaks` when one is installed and
# the pinned image through Docker otherwise; exits 127 when neither is available.
# Usage: scripts/gitleaks.sh [gitleaks git flags]
set -euo pipefail

version=8.30.1

cd "$(dirname "$0")/.."

if command -v gitleaks >/dev/null; then
    installed=$(gitleaks version)
    if [[ "${installed#v}" != "$version" ]]; then
        echo "gitleaks $installed is installed, but Flownodes pins $version." >&2
    fi
    exec gitleaks git --no-banner --redact --verbose "$@" .
fi

if command -v docker >/dev/null && docker info >/dev/null 2>&1; then
    exec docker run --rm -v "$PWD:/repo" "ghcr.io/gitleaks/gitleaks:v$version" \
        git --no-banner --redact --verbose "$@" /repo
fi

echo "Neither gitleaks nor Docker is available." >&2
exit 127
