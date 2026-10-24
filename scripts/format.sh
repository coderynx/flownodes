#!/usr/bin/env bash
# Applies the C# layout standard (ADR-0022) to every hand-written source file.
# With --check, fails when that changes anything, so CI proves the tree was formatted.
set -euo pipefail

cd "$(dirname "$0")/.."

dotnet tool restore > /dev/null

# The analyzers' warnings would fail ReSharper's project load, and its exit code is 4 whenever the solution reports
# any warning, so only a load or argument error counts as a failure.
status=0
dotnet jb cleanupcode Flownodes.slnx \
    --profile="Flownodes" \
    --include="**/*.cs" \
    --exclude="**/Database/Migrations/**;**/Internal/Generated/**" \
    --properties:"TreatWarningsAsErrors=false;EnforceCodeStyleInBuild=false" \
    --no-updates \
    > /dev/null || status=$?
if [[ $status -ne 0 && $status -ne 4 ]]; then
    echo "ReSharper cleanup failed with exit code $status." >&2
    exit "$status"
fi

# Namespaces matching folders and using-directive order, which ReSharper's cleanup cannot apply.
dotnet run scripts/Layout.cs

if [[ "${1:-}" == "--check" ]]; then
    if ! git diff --exit-code --stat -- '*.cs'; then
        echo "These files do not follow the layout standard. Run scripts/format.sh and commit the result." >&2
        exit 1
    fi
fi
