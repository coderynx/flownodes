## Project context

Product scope, audience and out-of-scope list: `README.md`. Read it before planning a feature, triaging an issue or proposing new scope.

**Pre-release, until further notice**: nothing is deployed and nobody depends on Flownodes yet. Change the API, the Resource and Manifest shapes, the Integration SDK, the database schema and stored data whenever it improves the design. Make the change directly, and leave out migrations, deprecation paths and compatibility shims. The SDK versioning promise in ADR-0003 starts at the first public release.

## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues (`coderynx/neo-flownodes`) via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Default vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
