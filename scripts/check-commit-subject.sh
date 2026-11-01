#!/usr/bin/env bash
# Checks that a commit subject follows Conventional Commits (ADR-0027). The commit-msg hook and the pull request title
# check in CI both call it, so the type and scope lists live here only.
# Usage: scripts/check-commit-subject.sh "<subject>"
set -euo pipefail

types='feat|fix|docs|refactor|perf|test|build|ci|chore|revert'
scopes='core|engine|identity|registry|sdk|flowctl|host|integrations|web|deps'
subject="${1-}"

if [[ ! "$subject" =~ ^($types)(\(($scopes)\))?!?:\ [^[:space:]] ]]; then
    echo "Subject does not follow Conventional Commits: $subject" >&2
    echo "Expected: <type>[(scope)][!]: <description>" >&2
    echo "Types: ${types//|/, }" >&2
    echo "Scopes: ${scopes//|/, }" >&2
    exit 1
fi

if [[ ${#subject} -gt 72 ]]; then
    echo "Subject is ${#subject} characters; keep it to 72: $subject" >&2
    exit 1
fi

if [[ "$subject" == *. ]]; then
    echo "Subject ends with a period: $subject" >&2
    exit 1
fi
