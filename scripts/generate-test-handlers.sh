#!/usr/bin/env bash
# Writes the Wolverine handler code each integration-test host loads (Internal/Generated/README.md in each project).
# Pass project directories to regenerate some; with none, every tests/*/*.IntegrationTests project is regenerated.
set -euo pipefail
cd "$(dirname "$0")/.."

projects=("$@")
if [ ${#projects[@]} -eq 0 ]; then
  projects=(tests/*/*.IntegrationTests)
fi

for project in "${projects[@]}"; do
  FLOWNODES_WRITE_HANDLERS=1 dotnet test --project "$project" --filter-class "*HandlerCodeGenerator"
done
