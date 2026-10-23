# Document exposed contracts only, and richly

XML docs belong only on types and members another project can access. Internal implementation types and members, including public members of internal types, have none: documenting them added noise without helping a caller. Visibility includes every containing type.

An exposed element carries a `<summary>`, a `<param>` for every parameter (a positional record's parameters included), a `<typeparam>` for every type parameter, a `<returns>` on everything that returns a value, and an `<exception>` for every exception it throws as part of its contract; exceptions that merely propagate from below are not listed. `<remarks>` appears only when there is more to say than the summary. An implementation or override of a documented contract uses `<inheritdoc/>`.

"Rich" means the doc tells a reader something the signature does not, in the language of `GLOSSARY.md`. "Gets or sets the name" restates the signature and does not count.

## Enforcement

StyleCop.Analyzers runs on every non-test project with only SA1600 (element documented), SA1611 (parameters), SA1615 (return value) and SA1618 (type parameters) switched on; `stylecop.json` sets `documentExposedElements` on and `documentInternalElements` off. The compiler's CS1591 covers exposed interface members, which StyleCop's `documentInterfaces` switch cannot reach without also documenting internal interfaces. `TreatWarningsAsErrors` makes a missing doc a build failure (ADR-0016). Test projects suppress CS1591 and are out of scope, as are generated migrations.

## Consequences

Tooling proves a doc is there and has its tags. It cannot prove the doc is rich, nor that `<exception>` and `<remarks>` are used as above; those are held by review, and a doc that only restates its signature is a review finding.
