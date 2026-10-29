// PROTOTYPE (#104) Variant A, "Explorer": one left sidebar holds everything. The Namespace
// switcher sits on top, the Asset tree is the primary navigation, configuration Kinds follow
// as a flat list, and Administration is pinned at the bottom for Administrators.
import { useState } from "react";
import { useAccess } from "./access";
import type { AssetNode } from "./data";
import { Content, openAlarmCount, sectionLabels, useAlarms, useAssetPath, useNamespaceData, type Nav, type Section } from "./shared";

export const name = "Explorer sidebar";

function Tree({ nodes, nav, depth }: { nodes: AssetNode[]; nav: Nav; depth: number }) {
  const [open, setOpen] = useState<Record<string, boolean>>({ fleet: true, "train-101": true, depot: true, "line-1": true, home: true });
  const { list } = useAlarms();
  return (
    <ul className="tree">
      {nodes.map((n) => {
        const alarmed = list.some((a) => !a.acknowledged && a.asset.startsWith(n.name));
        const selected = nav.place.section === "assets" && nav.place.asset === n.name;
        return (
          <li key={n.name}>
            <div className={`tree-row ${selected ? "selected" : ""}`} style={{ paddingLeft: depth * 14 + 6 }}>
              <span className="twisty" onClick={() => setOpen({ ...open, [n.name]: !open[n.name] })}>
                {n.children.length ? (open[n.name] ? "▾" : "▸") : "·"}
              </span>
              <a onClick={() => nav.go({ section: "assets", asset: n.name })}>{n.title}</a>
              {alarmed && <span className="dot red" title="Unacknowledged Alarm below" />}
            </div>
            {open[n.name] && n.children.length > 0 && <Tree nodes={n.children} nav={nav} depth={depth + 1} />}
          </li>
        );
      })}
    </ul>
  );
}

export function VariantA({ nav, setNamespace }: { nav: Nav; setNamespace: (ns: string) => void }) {
  const access = useAccess();
  const { tree } = useNamespaceData();
  const { list } = useAlarms();
  const path = useAssetPath(nav.place.asset);
  const link = (s: Section, badge?: number) => (
    <a key={s} className={`side-link ${nav.place.section === s ? "selected" : ""}`} onClick={() => nav.go({ section: s })}>
      {sectionLabels[s]}
      {badge ? <span className="count">{badge}</span> : null}
    </a>
  );
  return (
    <div className="a-shell">
      <aside className="a-side">
        <div className="a-brand">Flownodes</div>
        <label className="small muted">Namespace</label>
        <select value={access.namespace ?? ""} onChange={(e) => setNamespace(e.target.value)}>
          {access.visibleNamespaces.map((n) => (
            <option key={n} value={n}>
              {n} {access.persona.grants[n] ? `(${access.persona.grants[n].join(", ")})` : "(no role)"}
            </option>
          ))}
        </select>
        {access.canRead && (
          <>
            {link("alarms", openAlarmCount(list))}
            <div className="side-heading">Assets</div>
            <Tree nodes={tree} nav={nav} depth={0} />
            <div className="side-heading">Configuration</div>
            {link("flows")}
            {link("devices")}
            {link("connections")}
            {link("assetTypes")}
            {link("secrets")}
            {access.canEngineer && link("apply")}
          </>
        )}
        <div className="grow" />
        {access.canAdminister && (
          <>
            <div className="side-heading">Administration</div>
            {link("admin-namespaces")}
            {link("admin-users")}
          </>
        )}
        <div className="a-user small">
          Signed in as <b>{access.persona.name}</b> · Sign out
        </div>
      </aside>
      <main className="a-main">
        <div className="crumbs small muted">
          {access.namespace} / {sectionLabels[nav.place.section]}
          {path && path.map((p) => ` / ${p.title}`)}
          {nav.place.resource && ` / ${nav.place.resource.name}`}
        </div>
        <Content nav={nav} />
      </main>
    </div>
  );
}
