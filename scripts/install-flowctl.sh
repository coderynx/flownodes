#!/usr/bin/env bash
# Packs flowctl from this checkout and installs it as a global .NET tool, replacing any version already installed.
# Each pack gets its own prerelease version, so the tool and NuGet caches never serve an older build.
set -euo pipefail

cd "$(dirname "$0")/.."

packages=$(mktemp -d)
trap 'rm -rf "$packages"' EXIT

dotnet pack apps/Flowctl --configuration Release --output "$packages" --version-suffix "dev.$(date -u +%Y%m%d%H%M%S)"
dotnet tool update --global Flownodes.Flowctl --add-source "$packages" --prerelease --allow-downgrade
