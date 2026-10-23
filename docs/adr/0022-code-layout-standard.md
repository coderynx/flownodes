# C# is laid out to one standard

Every C# file wraps, breaks and braces the same way, so a diff shows a change rather than a reflow and an agent has one layout to imitate. The mechanical rules fail the build; the rest are held by review, because no formatter can tell where a method's logical steps begin.

## The standard

- **Namespaces**: each file uses its project's name followed by every directory below the project root, entry points included. A file holding only assembly attributes has no namespace.
- **Using directives**: ordinary directives sorted alphabetically with `System` first, then aliases sorted by alias. Only directives that are used.
- **Qualification**: no `this.`, and no containing type's name when the member is unambiguous in its scope.
- **Whitespace**: blank lines are empty, and at most one blank line in a row.
- **Line length**: at most 120 columns, wrapped at a sensible syntax boundary. A string literal or regex is never changed only to fit; a line whose overlength is a single unbreakable token (string, raw string, URL) is exempt. XML docs and comments are in scope.
- **General layout**: four-space indent, file-scoped namespaces, braces on their own line for types and control blocks, braces on every `if` body even for one statement.
- **Blank lines in a method**: keep a value beside its immediate validation, and consecutive assignments or related calls together. One blank line between distinct steps such as validation, construction, persistence and return. Separate setup from a following loop or `try`, and changes to tracked entities from `SaveChangesAsync`. No blank line just inside braces. Adjacent fields, auto-properties and enum values sit without blank lines.
- **Signatures**: one line if they fit. If they wrap, `(` follows the name and every parameter takes its own indented line. `: BaseType` stays with the declaration when it fits.
- **Arguments**: short calls stay inline. Once a call wraps, one argument per line, named arguments and cancellation tokens included. A logging call may keep its message template beside the method name.
- **Method chains**: short chains stay inline. A wrapped chain keeps the receiver and first call together (`db.Resources.Where(...)`); each later call starts an indented line with `.`.
- **Conditions**: an `if` with three or more predicates, mixed `&&` and `||`, or hard-to-read nesting is extracted into a named Boolean local just before it. Where extraction does not fit, one predicate per continuation line, operator at the end of the line.
- **Lambdas, initializers, collections, ternaries**: short ones stay inline. A wrapped expression lambda breaks after `=>`. A multi-property initializer puts `{` on its own line and one property per line. A collection that would pass 120 columns goes one element per line. A wrapped ternary starts each branch on its own line with `?` or `:` first.
- **Trailing commas**: none.

## Enforcement

`.editorconfig` sets the indent, compact member groups and ReSharper's wrapping preferences, and raises IDE0011 (braces), IDE0161 (file-scoped namespaces), IDE0001 and IDE0003 (qualification) to warnings, which the build turns into errors (ADR-0016). Architecture tests check namespaces, using order, empty and consecutive blank lines and line length under `apps/`, `packages/` and `tests/`, skipping `bin/`, `obj/` and `Database/Migrations/`. Changed files are reformatted with `dotnet jb cleanupcode Flownodes.slnx --profile="Built-in: Reformat Code" --include="<files;separated;by;semicolons>" --no-updates`, whose reformat-only profile neither reorders members nor changes syntax. Logical-step spacing, the complex-condition extraction, the logging-template exception and trailing commas are held by review.

StyleCop's own layout rules stay off: they cannot express the logical-step spacing or the logging-template exception.
