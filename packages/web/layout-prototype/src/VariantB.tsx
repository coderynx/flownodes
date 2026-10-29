// PROTOTYPE (#104) Variant B, "Console tabs": a Kubernetes-dashboard shape. The Namespace is
// the root of a breadcrumb in the top bar, sections are horizontal tabs, and each Namespace
// opens on an Overview. The Asset tree exists only inside the Assets tab, so the full width
// goes to tables everywhere else.
import { useAccess } from "./access";
import type { AssetNode } from "./data";
import {
  AlarmTable,
  Content,
  openAlarmCount,
  sectionLabels,
  useAlarms,
  useAssetPath,
  useNamespaceData,
  type Nav,
  type Section,
} from "./shared";

export const name = "Console tabs";

const tabs: Section[] = ["home", "assets", "alarms", "flows", "devices", "connections", "assetTypes", "secrets"];

function flatten(nodes: AssetNode[], depth = 0): { node: AssetNode; depth: number }[] {
  return nodes.flatMap((n) => [{ node: n, depth }, ...flatten(n.children, depth + 1)]);
}

function Overview({ nav }: { nav: Nav }) {
  const { rows, tree } = useNamespaceData();
  const { list } = useAlarms();
  const reconciling = rows.filter((r) => r.reconciled < r.generation);
  const offline = rows.filter((r) => r.status === "Offline");
  const bad = flatten(tree).filter(({ node }) => node.dataPoints.some((d) => d.quality === "bad"));
  return (
    <div className="stack">
      <div className="b-tiles">
        <div className="tile" onClick={() => nav.go({ section: "alarms" })}>
          <div className="big">{openAlarmCount(list)}</div>unacknowledged Alarms
        </div>
        <div className="tile" onClick={() => nav.go({ section: "assets" })}>
          <div className="big">{bad.length}</div>Assets with bad Quality
        </div>
        <div className="tile" onClick={() => nav.go({ section: "devices" })}>
          <div className="big">{offline.length}</div>Devices offline
        </div>
        <div className="tile">
          <div className="big">{reconciling.length}</div>Resources reconciling
        </div>
      </div>
      <section>
        <h3>Alarms</h3>
        <AlarmTable compact nav={nav} />
      </section>
      <section>
        <h3>Needs attention</h3>
        <ul>
          {bad.map(({ node }) => (
            <li key={node.name}>
              <a onClick={() => nav.go({ section: "assets", asset: node.name })}>{node.title}</a> has a bad Data Point
            </li>
          ))}
          {reconciling.map((r) => (
            <li key={r.name}>
              <span className="mono">
                {r.kind}/{r.name}
              </span>{" "}
              is reconciling Generation {r.generation}
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}

export function VariantB({ nav, setNamespace }: { nav: Nav; setNamespace: (ns: string) => void }) {
  const access = useAccess();
  const { tree } = useNamespaceData();
  const { list } = useAlarms();
  const path = useAssetPath(nav.place.asset);
  const admin = nav.place.section.startsWith("admin");
  return (
    <div className="b-shell">
      <header className="b-top">
        <b>Flownodes</b>
        <span className="muted">/</span>
        {admin ? (
          <span>Administration</span>
        ) : (
          <select value={access.namespace ?? ""} onChange={(e) => setNamespace(e.target.value)}>
            {access.visibleNamespaces.map((n) => (
              <option key={n} value={n}>
                {n}
              </option>
            ))}
          </select>
        )}
        <span className="muted">/</span>
        <span>{sectionLabels[nav.place.section]}</span>
        {path?.map((p) => (
          <span key={p.name}>
            <span className="muted">/ </span>
            <a onClick={() => nav.go({ section: "assets", asset: p.name })}>{p.title}</a>
          </span>
        ))}
        {nav.place.resource && <span className="mono"> / {nav.place.resource.name}</span>}
        <div className="grow" />
        {access.canEngineer && !admin && (
          <button className="primary" onClick={() => nav.go({ section: "apply" })}>
            + Apply
          </button>
        )}
        {access.canAdminister && (
          <button onClick={() => nav.go({ section: admin ? "home" : "admin-users" })}>{admin ? "← Namespaces" : "⚙ Administration"}</button>
        )}
        <span className="small">
          {access.persona.name}
          {access.namespace && access.persona.grants[access.namespace] && !admin && (
            <span className="muted"> · {access.persona.grants[access.namespace].join(", ")}</span>
          )}
        </span>
      </header>
      <nav className="b-tabs">
        {admin
          ? (["admin-users", "admin-namespaces"] as Section[]).map((s) => (
              <a key={s} className={nav.place.section === s ? "selected" : ""} onClick={() => nav.go({ section: s })}>
                {sectionLabels[s]}
              </a>
            ))
          : access.canRead &&
            tabs.map((s) => (
              <a key={s} className={nav.place.section === s ? "selected" : ""} onClick={() => nav.go({ section: s })}>
                {sectionLabels[s]}
                {s === "alarms" && openAlarmCount(list) > 0 && <span className="count">{openAlarmCount(list)}</span>}
              </a>
            ))}
      </nav>
      <main className="b-main">
        {nav.place.section === "home" && access.canRead ? (
          <Overview nav={nav} />
        ) : nav.place.section === "assets" && access.canRead ? (
          <div className="b-assets">
            <div className="b-tree">
              <input placeholder="Filter Assets…" />
              {flatten(tree).map(({ node, depth }) => (
                <a
                  key={node.name}
                  className={nav.place.asset === node.name ? "selected" : ""}
                  style={{ paddingLeft: depth * 12 + 6 }}
                  onClick={() => nav.go({ section: "assets", asset: node.name })}
                >
                  {node.title}
                </a>
              ))}
            </div>
            <div className="grow">{path ? <Content nav={nav} /> : <p className="muted">Pick an Asset.</p>}</div>
          </div>
        ) : (
          <Content nav={nav} />
        )}
      </main>
    </div>
  );
}
