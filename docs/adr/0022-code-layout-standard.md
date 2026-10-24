# C# is laid out by scripts, never by hand

Every C# file wraps, breaks and braces the same way, so a diff shows a change rather than a reflow. The layout is whatever `scripts/format.sh` produces: an agent or a human runs it and never arranges code by hand, and no rule is held by review alone. A rule a script can apply is applied by one; a rule only a script can detect fails a test at the offending line and is fixed there; a rule neither can check is not part of the standard.

## The standard

Applied by `scripts/format.sh`:

- **Namespaces**: each file uses its project's name followed by every directory below the project root, entry points included. A file holding only assembly attributes has no namespace.
- **Using directives**: ordinary directives sorted alphabetically with `System` first, then aliases sorted by alias. Only directives that are used.
- **Qualification**: no `this.`, and no containing type's name when the member is unambiguous in its scope.
- **Syntax**: file-scoped namespaces, braces on every control-statement body, no trailing commas.
- **Whitespace and wrapping**: four-space indent, blank lines empty and never two in a row, none just inside braces or between adjacent fields, auto-properties and enum values. Lines wrap at 120 columns with one parameter or argument per line once a signature or call wraps, and a wrapped chain keeps the receiver and first call together. The `resharper_*` keys in `.editorconfig` define the wrapping; where they and a reader disagree, the keys win.

Detected by `tests/Architecture.Tests`:

- **Line length**: a line the formatter cannot wrap, such as a long comment, is wrapped by whoever wrote it. A line whose overlength is a single unbreakable token (string, raw string, URL) is exempt.
- **Conditions**: an `if` whose condition joins three or more predicates with `&&` or `||` names them in a Boolean local just before it.

Blank lines between a method's steps are kept as written and not judged: no script can tell where a step begins.

## Enforcement

`scripts/format.sh` runs ReSharper's command-line cleanup with the `Flownodes` profile from `Flownodes.slnx.DotSettings` (reformat, optimise usings, qualifiers, braces, namespaces, trailing commas) over the `.cs` files it is given, or every one outside `Database/Migrations/` and `Internal/Generated/`, then `scripts/Layout.cs`, which fixes namespaces and using order. It reads the project model without building (`--no-build`): loading it costs about 25 seconds and each file a fraction of a second more. `scripts/format.sh --check` fails CI when the committed tree differs from its output, in a job beside the build, and the `.githooks/pre-commit` hook runs it on a commit's staged files once a clone sets `git config core.hooksPath .githooks`. The build still fails on IDE0011 (braces), IDE0161 (file-scoped namespaces) and IDE0001 and IDE0003 (qualification) through ADR-0016, so a missed run is caught before CI.

StyleCop's own layout rules stay off: the formatter owns layout, and two tools ruling it would fight.

## Considered

A logical-step spacing rule, a logging-template exception to argument wrapping and trailing-comma removal were once held by review. Review missed trailing commas in practice, and an agent spending turns rearranging code that a script can arrange is the cost this ADR removes. Trailing commas moved to the formatter; the other two were dropped.
