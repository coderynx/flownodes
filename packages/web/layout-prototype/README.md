# PROTOTYPE: layout and navigation (#104)

Throwaway. This answers "what does the web UI's overall layout look like?" and is not the start of `Web.Client`.

```sh
cd packages/web/layout-prototype && npm install && npm run dev
```

The pink bar at the bottom is the prototype's control, not part of the design:

- `←` / `→` (or the arrow keys) switch between the variants (`?variant=A|B|C`).
  - **A, Explorer sidebar**: one dark sidebar holds the Namespace select, the Asset tree as primary navigation, the configuration Kinds and Administration.
  - **B, Console tabs**: the Namespace is the root of a top breadcrumb, sections are tabs, each Namespace opens on an Overview, and the Asset tree appears only under Assets.
  - **C, Operate / Configure**: the UI splits by job into Operate (Asset tree, live values, a permanent Alarm rail), Configure (Resources by Kind) and Administer. It has a full-page Namespace chooser and a Ctrl+K palette.
- The persona select (`?persona=`) switches between Viewer, Operator, Engineer, Operator + Engineer, Administrator only, and Administrator + Namespace roles.
- The gating select (`?gating=hide|disable`) compares hiding what a role can't do with showing it disabled, an open question on the map.

Assumptions: any Namespace role reads everything in it (#80), and an Administrator without a Role Grant in a Namespace sees none of its content. The data is in memory; only acknowledging an Alarm changes anything.
