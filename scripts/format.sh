#!/usr/bin/env bash
# Applies the C# layout standard (ADR-0022) to the given .cs files, or to every hand-written one when none are given.
# With --check, fails when that changes anything, so CI proves the tree was formatted.
set -euo pipefail

check=false
if [[ "${1:-}" == "--check" ]]; then
    check=true
    shift
fi

cd "$(dirname "$0")/.."

include="**/*.cs"
if [[ $# -gt 0 ]]; then
    include=$(IFS=';'; echo "$*")
fi

dotnet tool restore > /dev/null

# --no-build: formatting needs the project model, not compiled output.
dotnet jb cleanupcode Flownodes.slnx \
    --profile="Flownodes" \
    --include="$include" \
    --exclude="**/Database/Migrations/**;**/Internal/Generated/**" \
    --no-build \
    --no-updates \
    > /dev/null

# Namespaces matching folders and using-directive order, which ReSharper's cleanup cannot apply.
dotnet run scripts/Layout.cs

if [[ "$check" == true ]] && ! git diff --exit-code --stat -- '*.cs'; then
    echo "These files do not follow the layout standard. Run scripts/format.sh and commit the result." >&2
    exit 1
fi
