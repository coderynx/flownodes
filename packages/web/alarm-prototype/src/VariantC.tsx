// PROTOTYPE (#104) Variant C, "Operate / Configure", reshaped after the ESXi Host Client:
// a plain console for this one Installation, leaving fleet-wide richness to the Control Center
// the way ESXi leaves it to vCenter. The Installation is a root object with its own page, every
// object page has a header, an action toolbar and tabs, and a Recent changes panel at the
// bottom shows each apply until it is reconciled. Visuals follow ISA-101: the screen stays
// grey and only abnormal situations get colour, always paired with a shape or a word.
import { useEffect, useMemo, useState, type ReactNode } from "react";
import "@fontsource/atkinson-hyperlegible-next/400.css";
import "@fontsource/atkinson-hyperlegible-next/600.css";
import "@fontsource/atkinson-hyperlegible-next/700.css";
import "@fontsource/atkinson-hyperlegible-mono/400.css";
import "./variant-c.css";
import { AlarmsPage } from "./AlarmViews";
import { Gate, useAccess } from "./access";
import { findAsset, installation, recentChanges, type Alarm, type AssetNode, type ResourceRow } from "./data";
import {
  AdminNamespaces,
  AdminUsers,
  ApplyPage,
  DataPointTable,
  kindSection,
  sectionKind,
  sectionLabels,
  useAlarms,
  useNamespaceData,
  type Nav,
  type Place,
  type Section,
} from "./shared";

export const name = "Operate / Configure (ESXi-shaped)";

type Mode = "operate" | "configure" | "installation";

const configureSections: Section[] = ["flows", "devices", "connections", "assetTypes", "secrets"];
const installationSections: Section[] = ["installation", "admin-namespaces", "admin-users"];

function modeOf(section: Section): Mode {
  if (installationSections.includes(section)) return "installation";
  if (configureSections.includes(section) || section === "apply") return "configure";
  return "operate";
}

function flatten(nodes: AssetNode[]): AssetNode[] {
  return nodes.flatMap((n) => [n, ...flatten(n.children)]);
}

const rank: Record<Alarm["priority"], number> = { critical: 4, high: 3, medium: 2, low: 1 };

function worstUnder(list: Alarm[], node: AssetNode): Alarm | undefined {
  const names = new Set(flatten([node]).map((n) => n.name));
  return list
    .filter((a) => names.has(a.asset) && (!a.acknowledged || a.active))
    .sort((x, y) => Number(x.acknowledged) - Number(y.acknowledged) || rank[y.priority] - rank[x.priority])[0];
}

// Each Alarm Priority has its own shape, so it reads without colour. Unacknowledged is
// solid, acknowledged is an outline; an unacknowledged critical Alarm pulses.
export function PriorityGlyph({ alarm, size = 14 }: { alarm: Pick<Alarm, "priority" | "acknowledged">; size?: number }) {
  const shapes: Record<Alarm["priority"], ReactNode> = {
    critical: <path d="M8 1 15 8 8 15 1 8Z" />,
    high: <path d="M8 1.5 15 14.5H1Z" />,
    medium: <rect x="2" y="2" width="12" height="12" />,
    low: <circle cx="8" cy="8" r="6" />,
  };
  return (
    <svg
      className={`glyph g-${alarm.priority} ${alarm.acknowledged ? "acked" : "unacked"}`}
      width={size}
      height={size}
      viewBox="0 0 16 16"
      role="img"
      aria-label={`${alarm.priority} priority, ${alarm.acknowledged ? "acknowledged" : "unacknowledged"}`}
    >
      {shapes[alarm.priority]}
    </svg>
  );
}

function Tree({ nodes, nav, depth }: { nodes: AssetNode[]; nav: Nav; depth: number }) {
  const { list } = useAlarms();
  return (
    <ul role={depth === 0 ? "tree" : "group"}>
      {nodes.map((n) => {
        const worst = worstUnder(list, n);
        const bad = n.dataPoints.some((d) => d.quality === "bad");
        const selected = nav.place.asset === n.name;
        return (
          <li key={n.name} role="treeitem" aria-selected={selected}>
            <button
              className={`nav-item ${selected ? "selected" : ""}`}
              style={{ paddingLeft: depth * 16 + 12 }}
              onClick={() => nav.go({ section: "assets", asset: n.name })}
            >
              <span className="nav-label">{n.title}</span>
              {bad && <span className="bad-mark" title="A Data Point has bad Quality">bad</span>}
              {worst && <PriorityGlyph alarm={worst} size={12} />}
            </button>
            {n.children.length > 0 && <Tree nodes={n.children} nav={nav} depth={depth + 1} />}
          </li>
        );
      })}
    </ul>
  );
}

function NavGroup({ items, nav, counts }: { items: Section[]; nav: Nav; counts?: Partial<Record<Section, number>> }) {
  return (
    <ul>
      {items.map((s) => (
        <li key={s}>
          <button className={`nav-item ${nav.place.section === s ? "selected" : ""}`} onClick={() => nav.go({ section: s })}>
            <span className="nav-label">{sectionLabels[s]}</span>
            {counts?.[s] !== undefined && <span className="nav-count">{counts[s]}</span>}
          </button>
        </li>
      ))}
    </ul>
  );
}

function ObjectPage({
  kind,
  title,
  identifier,
  state,
  toolbar,
  tabs,
}: {
  kind: string;
  title: string;
  identifier?: string;
  state?: ReactNode;
  toolbar?: ReactNode;
  tabs: { key: string; label: string; body: ReactNode }[];
}) {
  const [tab, setTab] = useState(tabs[0].key);
  const current = tabs.find((t) => t.key === tab) ?? tabs[0];
  return (
    <article className="object">
      <header className="object-head">
        <div>
          <p className="object-kind">{kind}</p>
          <h1>{title}</h1>
          {identifier && <p className="ident">{identifier}</p>}
        </div>
        {state && <div className="object-state">{state}</div>}
      </header>
      {toolbar && <div className="toolbar">{toolbar}</div>}
      {tabs.length > 1 && (
        <div className="tabs" role="tablist">
          {tabs.map((t) => (
            <button key={t.key} role="tab" aria-selected={t.key === current.key} onClick={() => setTab(t.key)}>
              {t.label}
            </button>
          ))}
        </div>
      )}
      <div className="object-body">{current.body}</div>
    </article>
  );
}

function AlarmList({ alarms, nav, showAsset }: { alarms: Alarm[]; nav: Nav; showAsset?: boolean }) {
  const { canAcknowledge } = useAccess();
  const { acknowledge } = useAlarms();
  if (alarms.length === 0) return <p className="empty">No Alarms. Abnormal situations on this Asset show up here.</p>;
  return (
    <table className="grid">
      <thead>
        <tr>
          <th>Priority</th>
          <th>Alarm</th>
          {showAsset && <th>Asset</th>}
          <th>Condition</th>
          <th>Raised</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {alarms.map((a) => (
          <tr key={a.id} className={a.acknowledged ? "quiet" : undefined}>
            <td>
              <span className="prio">
                <PriorityGlyph alarm={a} /> {a.priority}
              </span>
            </td>
            <td>{a.message}</td>
            {showAsset && (
              <td>
                <button className="link ident" onClick={() => nav.go({ section: "assets", asset: a.asset })}>
                  {a.asset}
                </button>
              </td>
            )}
            <td>
              {a.active ? "Active" : "Cleared"}, {a.acknowledged ? "acknowledged" : "unacknowledged"}
            </td>
            <td className="num">{a.raised}</td>
            <td className="end">
              {!a.acknowledged && (
                <Gate allowed={canAcknowledge} need="Operator">
                  <button className="btn" disabled={!canAcknowledge} onClick={() => acknowledge(a.id)}>
                    Acknowledge
                  </button>
                </Gate>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function ToolButton({ allowed, need, children, primary }: { allowed: boolean; need: string; children: ReactNode; primary?: boolean }) {
  return (
    <Gate allowed={allowed} need={need}>
      <button className={`btn ${primary ? "primary" : ""}`} disabled={!allowed}>
        {children}
      </button>
    </Gate>
  );
}

function Revisions({ generation }: { generation: number }) {
  const who = ["cara", "ben", "fay"];
  return (
    <table className="grid">
      <thead>
        <tr>
          <th>Generation</th>
          <th>Applied by</th>
          <th>When</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {Array.from({ length: Math.min(generation, 4) }, (_, i) => generation - i).map((g, i) => (
          <tr key={g}>
            <td className="num">{g}</td>
            <td>{who[i % who.length]}</td>
            <td>{i === 0 ? "today 13:52" : `${i * 3} days ago`}</td>
            <td className="end">
              <button className="btn">Compare with current</button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function Manifest({ yaml }: { yaml: string }) {
  const { canEngineer } = useAccess();
  return (
    <div className="stack">
      <pre className="manifest">{yaml}</pre>
      {!canEngineer && <p className="hint">You can read this Manifest. Changing it needs the Engineer role.</p>}
    </div>
  );
}

function AssetPage({ path, nav }: { path: AssetNode[]; nav: Nav }) {
  const { canOperate, canEngineer, namespace } = useAccess();
  const { list } = useAlarms();
  const asset = path[path.length - 1];
  const alarms = list.filter((a) => flatten([asset]).some((n) => n.name === a.asset));
  const bad = asset.dataPoints.filter((d) => d.quality === "bad").length;
  return (
    <ObjectPage
      key={asset.name}
      kind={asset.type ? `Asset, instance of ${asset.type}` : "Asset"}
      title={asset.title}
      identifier={`Asset/${asset.name}`}
      state={
        <>
          {alarms.filter((a) => !a.acknowledged).length > 0 && (
            <span className="state alarm">{alarms.filter((a) => !a.acknowledged).length} unacknowledged</span>
          )}
          {bad > 0 && <span className="state bad">{bad} bad Quality</span>}
          {alarms.length === 0 && bad === 0 && <span className="state">Normal</span>}
        </>
      }
      toolbar={
        <>
          {asset.actions.map((a) => (
            <ToolButton key={a} allowed={canOperate} need="Operator">
              {a}
            </ToolButton>
          ))}
          {asset.actions.length > 0 && <span className="tool-sep" />}
          <ToolButton allowed={canEngineer} need="Engineer">
            Edit Manifest
          </ToolButton>
          <ToolButton allowed={canEngineer} need="Engineer">
            Attach a Device
          </ToolButton>
        </>
      }
      tabs={[
        {
          key: "live",
          label: "Data Points",
          body: <DataPointTable asset={asset} />,
        },
        { key: "alarms", label: `Alarms (${alarms.length})`, body: <AlarmList alarms={alarms} nav={nav} showAsset={asset.children.length > 0} /> },
        {
          key: "devices",
          label: "Devices",
          body:
            asset.devices.length === 0 ? (
              <p className="empty">No Device is attached here. Child Assets may have their own.</p>
            ) : (
              <ul className="plain">
                {asset.devices.map((d) => (
                  <li key={d}>
                    <button className="link ident" onClick={() => nav.go({ section: "devices", resource: { kind: "Device", name: d } })}>
                      Device/{d}
                    </button>
                  </li>
                ))}
              </ul>
            ),
        },
        {
          key: "manifest",
          label: "Manifest",
          body: (
            <Manifest
              yaml={`apiVersion: flownodes/v1\nkind: Asset\nmetadata:\n  name: ${asset.name}\n  namespace: ${namespace}\nspec:\n${asset.type ? `  type: ${asset.type}\n` : ""}  dataPoints:\n${asset.dataPoints.map((d) => `    - name: ${d.name}\n      from: ${d.source}`).join("\n")}`}
            />
          ),
        },
        { key: "revisions", label: "Revisions", body: <Revisions generation={3} /> },
      ]}
    />
  );
}

function fakeSpec(r: ResourceRow) {
  if (r.kind === "Secret") return "  values:\n    username: (write-only)\n    password: (write-only)";
  if (r.kind === "Flow") return "  executionMode: queue\n  nodes:\n    - id: trigger\n      event: depot-mqtt/timetable\n    - id: assign\n      ...";
  return `  # ${r.detail}`;
}

function ResourcePage({ kind, name, nav }: { kind: string; name: string; nav: Nav }) {
  const { canEngineer, canOperate, namespace } = useAccess();
  const { rows } = useNamespaceData();
  const r = rows.find((x) => x.kind === kind && x.name === name);
  if (!r) return <p className="empty">There is no {kind} named {name} in this Namespace.</p>;
  const reconciling = r.reconciled < r.generation;
  const tabs = [
    {
      key: "summary",
      label: "Summary",
      body: (
        <dl className="facts">
          <dt>Status</dt>
          <dd>{r.status}</dd>
          <dt>Generation</dt>
          <dd>
            {r.generation}
            {reconciling ? `, Engine still on ${r.reconciled}` : ", reconciled"}
          </dd>
          <dt>Detail</dt>
          <dd>{r.detail}</dd>
        </dl>
      ),
    },
    ...(kind === "Flow"
      ? [
          { key: "graph", label: "Graph", body: <div className="graph">Read-only graph of the Flow's Nodes, laid out by dagre</div> },
          {
            key: "executions",
            label: "Flow Executions",
            body: (
              <table className="grid">
                <tbody>
                  <tr>
                    <td className="num">4183</td>
                    <td>Running, waiting at Node assign</td>
                    <td>Generation 8</td>
                  </tr>
                  <tr className="quiet">
                    <td className="num">4182</td>
                    <td>Completed 13:50</td>
                    <td>Generation 8</td>
                  </tr>
                </tbody>
              </table>
            ),
          },
        ]
      : []),
    {
      key: "manifest",
      label: "Manifest",
      body: <Manifest yaml={`apiVersion: flownodes/v1\nkind: ${kind}\nmetadata:\n  name: ${name}\n  namespace: ${namespace}\nspec:\n${fakeSpec(r)}`} />,
    },
    { key: "revisions", label: "Revisions", body: <Revisions generation={r.generation} /> },
  ];
  return (
    <ObjectPage
      key={`${kind}/${name}`}
      kind={kind}
      title={name}
      identifier={`${kind}/${name}`}
      state={<span className={`state ${r.status === "Offline" ? "bad" : reconciling ? "pending" : ""}`}>{reconciling ? "Reconciling" : r.status}</span>}
      toolbar={
        <>
          {kind === "Connection" && (
            <>
              <ToolButton allowed={canOperate} need="Operator">
                Pair
              </ToolButton>
              <span className="tool-sep" />
            </>
          )}
          <ToolButton allowed={canEngineer} need="Engineer">
            Edit Manifest
          </ToolButton>
          {kind === "Secret" && (
            <ToolButton allowed={canEngineer} need="Engineer">
              Replace values
            </ToolButton>
          )}
          <ToolButton allowed={canEngineer} need="Engineer">
            Delete
          </ToolButton>
          <span className="grow" />
          <button className="link" onClick={() => nav.go({ section: kindSection[kind] })}>
            All {sectionLabels[kindSection[kind]]}
          </button>
        </>
      }
      tabs={tabs}
    />
  );
}

function KindPage({ section, nav }: { section: Section; nav: Nav }) {
  const { canEngineer } = useAccess();
  const { rows, discoveries } = useNamespaceData();
  const kind = sectionKind[section]!;
  const list = rows.filter((r) => r.kind === kind);
  const table = (
    <table className="grid">
      <thead>
        <tr>
          <th>Name</th>
          <th>Status</th>
          <th>Generation</th>
          <th>Detail</th>
        </tr>
      </thead>
      <tbody>
        {list.map((r) => (
          <tr key={r.name}>
            <td>
              <button className="link ident" onClick={() => nav.go({ section, resource: { kind, name: r.name } })}>
                {r.name}
              </button>
            </td>
            <td className={r.status === "Offline" ? "bad-text" : undefined}>{r.reconciled < r.generation ? "Reconciling" : r.status}</td>
            <td className="num">{r.generation}</td>
            <td className="dim">{r.detail}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
  return (
    <ObjectPage
      key={section}
      kind="Resources in this Namespace"
      title={sectionLabels[section]}
      toolbar={
        <>
          <Gate allowed={canEngineer} need="Engineer">
            <button className="btn primary" disabled={!canEngineer} onClick={() => nav.go({ section: "apply" })}>
              New {kind}
            </button>
          </Gate>
        </>
      }
      tabs={[
        { key: "list", label: "All", body: list.length ? table : <p className="empty">No {sectionLabels[section]} yet.</p> },
        ...(kind === "Device"
          ? [
              {
                key: "discoveries",
                label: `Discoveries (${discoveries.length})`,
                body:
                  discoveries.length === 0 ? (
                    <p className="empty">Every unit the Connections can reach is already a Device.</p>
                  ) : (
                    <table className="grid">
                      <tbody>
                        {discoveries.map((d) => (
                          <tr key={d}>
                            <td>{d}</td>
                            <td className="end">
                              <Gate allowed={canEngineer} need="Engineer">
                                <button className="btn" disabled={!canEngineer}>
                                  Declare as Device
                                </button>
                              </Gate>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  ),
              },
            ]
          : []),
      ]}
    />
  );
}

function Meter({ value, label }: { value: number; label: string }) {
  return (
    <div className="meter" role="meter" aria-valuenow={value} aria-valuemin={0} aria-valuemax={100} aria-label={label}>
      <span style={{ width: `${value}%` }} />
      <em>{value} %</em>
    </div>
  );
}

function InstallationPage() {
  const { canAdminister } = useAccess();
  return (
    <ObjectPage
      kind="Installation"
      title={installation.name}
      identifier={installation.id}
      state={<span className="state">Running on {installation.silos.length} Silos</span>}
      toolbar={
        <>
          <ToolButton allowed={canAdminister} need="Administrator">
            Enrol in a Control Center
          </ToolButton>
          <ToolButton allowed={canAdminister} need="Administrator">
            New Namespace
          </ToolButton>
        </>
      }
      tabs={[
        {
          key: "summary",
          label: "Summary",
          body: (
            <div className="stack">
              <dl className="facts">
                <dt>Flownodes version</dt>
                <dd className="ident">{installation.version}</dd>
                <dt>Up for</dt>
                <dd>{installation.uptime}</dd>
                <dt>Control Center</dt>
                <dd>{installation.controlCenter ?? "Not enrolled. This Installation works fully on its own."}</dd>
              </dl>
              <section>
                <h2>Silos</h2>
                <table className="grid">
                  <thead>
                    <tr>
                      <th>Silo</th>
                      <th>Address</th>
                      <th>State</th>
                      <th>CPU</th>
                      <th>Memory</th>
                    </tr>
                  </thead>
                  <tbody>
                    {installation.silos.map((s) => (
                      <tr key={s.name}>
                        <td className="ident">{s.name}</td>
                        <td className="ident dim">{s.address}</td>
                        <td>{s.state}</td>
                        <td>
                          <Meter value={s.cpu} label={`${s.name} CPU`} />
                        </td>
                        <td>
                          <Meter value={s.memory} label={`${s.name} memory`} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </section>
            </div>
          ),
        },
        {
          key: "integrations",
          label: "Integrations",
          body: (
            <table className="grid">
              <thead>
                <tr>
                  <th>Integration</th>
                  <th>Version</th>
                  <th>Device Types</th>
                </tr>
              </thead>
              <tbody>
                {installation.integrations.map((i) => (
                  <tr key={i.name}>
                    <td className="ident">{i.name}</td>
                    <td className="num">{i.version}</td>
                    <td className="num">{i.deviceTypes || "none, External Service only"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ),
        },
      ]}
    />
  );
}

function NamespaceChooser({ onPick }: { onPick: (ns: string) => void }) {
  const access = useAccess();
  return (
    <div className="chooser">
      <h1>Choose a Namespace</h1>
      <p className="hint">You see the Namespaces where you hold a role{access.canAdminister ? ", and as an Administrator, every Namespace" : ""}.</p>
      <ul className="plain">
        {access.visibleNamespaces.map((n) => (
          <li key={n}>
            <button className={`chooser-row ${n === access.namespace ? "selected" : ""}`} onClick={() => onPick(n)}>
              <span className="ident">{n}</span>
              <span>{access.persona.grants[n]?.join(" and ") ?? "No role, so its content stays hidden"}</span>
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

function Palette({ nav, close, setNamespace }: { nav: Nav; close: () => void; setNamespace: (ns: string) => void }) {
  const access = useAccess();
  const { tree, rows } = useNamespaceData();
  const [q, setQ] = useState("");
  const items = useMemo(() => {
    const all: { label: string; hint: string; run: () => void }[] = [
      ...(access.canRead ? flatten(tree) : []).map((a) => ({ label: a.title, hint: `Asset/${a.name}`, run: () => nav.go({ section: "assets", asset: a.name }) })),
      ...(access.canRead ? rows : []).map((r) => ({
        label: r.name,
        hint: `${r.kind}/${r.name}`,
        run: () => nav.go({ section: kindSection[r.kind], resource: { kind: r.kind, name: r.name } }),
      })),
      ...access.visibleNamespaces.map((n) => ({ label: `Switch to ${n}`, hint: "Namespace", run: () => setNamespace(n) })),
    ];
    return all.filter((i) => `${i.label} ${i.hint}`.toLowerCase().includes(q.toLowerCase())).slice(0, 10);
  }, [q, tree, rows, access, nav, setNamespace]);
  return (
    <div className="palette-backdrop" onClick={close}>
      <div className="palette" role="dialog" aria-label="Go to" onClick={(e) => e.stopPropagation()}>
        <input
          autoFocus
          placeholder="Go to an Asset, Resource or Namespace"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Escape") close();
            if (e.key === "Enter" && items[0]) {
              items[0].run();
              close();
            }
          }}
        />
        <ul className="plain">
          {items.map((i) => (
            <li key={i.hint + i.label}>
              <button
                onClick={() => {
                  i.run();
                  close();
                }}
              >
                {i.label}
                <span className="ident dim">{i.hint}</span>
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

function RecentChanges() {
  const access = useAccess();
  const [open, setOpen] = useState(false);
  const visible = recentChanges.filter((c) =>
    c.namespace === null ? access.canAdminister : access.persona.grants[c.namespace] !== undefined,
  );
  const pending = visible.filter((c) => c.state === "Reconciling").length;
  return (
    <section className={`changes ${open ? "open" : ""}`} aria-label="Recent changes">
      <button className="changes-toggle" aria-expanded={open} onClick={() => setOpen(!open)}>
        Recent changes
        {pending > 0 && <span className="state pending">{pending} reconciling</span>}
        <span className="grow" />
        <span aria-hidden>{open ? "▾" : "▴"}</span>
      </button>
      {open && (
        <table className="grid dense">
          <thead>
            <tr>
              <th>When</th>
              <th>Who</th>
              <th>Namespace</th>
              <th>Change</th>
              <th>Result</th>
            </tr>
          </thead>
          <tbody>
            {visible.map((c) => (
              <tr key={c.at + c.what}>
                <td className="num">{c.at}</td>
                <td>{c.who}</td>
                <td className="ident">{c.namespace ?? "Installation"}</td>
                <td className="ident">
                  {c.what}
                  {c.generation && <span className="dim"> to Generation {c.generation}</span>}
                </td>
                <td className={c.state === "Rejected" ? "bad-text" : c.state === "Reconciling" ? "pending-text" : undefined}>
                  {c.state}
                  {c.note && <span className="dim">: {c.note}</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

export function VariantC({ nav, setNamespace }: { nav: Nav; setNamespace: (ns: string) => void }) {
  const access = useAccess();
  const { tree, rows } = useNamespaceData();
  const { list } = useAlarms();
  const [choosing, setChoosing] = useState(false);
  const [palette, setPalette] = useState(false);
  const mode = modeOf(nav.place.section);
  const unacked = list.filter((a) => !a.acknowledged).length;
  const path = nav.place.asset ? findAsset(tree, nav.place.asset) : null;

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === "k") {
        e.preventDefault();
        setPalette(true);
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  const modes: { key: Mode; label: string; first: Place; show: boolean }[] = [
    { key: "operate", label: "Operate", first: { section: "assets", asset: tree[0]?.name }, show: access.canRead },
    { key: "configure", label: "Configure", first: { section: "flows" }, show: access.canRead },
    { key: "installation", label: "Installation", first: { section: "installation" }, show: true },
  ];

  const counts = Object.fromEntries(configureSections.map((s) => [s, rows.filter((r) => r.kind === sectionKind[s]).length]));

  let main: ReactNode;
  if (choosing)
    main = (
      <NamespaceChooser
        onPick={(ns) => {
          setNamespace(ns);
          setChoosing(false);
        }}
      />
    );
  else if (nav.place.section === "installation") main = <InstallationPage />;
  else if (nav.place.section === "admin-namespaces") main = <ObjectPage kind="Installation" title="Namespaces" tabs={[{ key: "x", label: "", body: <AdminNamespaces /> }]} />;
  else if (nav.place.section === "admin-users") main = <ObjectPage kind="Installation" title="Users and Role Grants" tabs={[{ key: "x", label: "", body: <AdminUsers /> }]} />;
  else if (!access.canRead)
    main = (
      <p className="empty">
        You hold no role in {access.namespace}, so its content stays hidden. An Administrator can grant you one under Users and Role Grants.
      </p>
    );
  else if (nav.place.section === "assets" && path) main = <AssetPage path={path} nav={nav} />;
  else if (nav.place.section === "alarms") main = <AlarmsPage nav={nav} />;
  else if (nav.place.section === "apply") main = <ObjectPage kind="Configure" title="Apply a Manifest" tabs={[{ key: "x", label: "", body: <ApplyPage /> }]} />;
  else if (nav.place.resource) main = <ResourcePage kind={nav.place.resource.kind} name={nav.place.resource.name} nav={nav} />;
  else if (sectionKind[nav.place.section]) main = <KindPage section={nav.place.section} nav={nav} />;
  else main = <p className="empty">Pick an Asset in the tree to see its live Data Points.</p>;

  return (
    <div className="esx">
      <header className="masthead">
        <button className="wordmark" onClick={() => nav.go({ section: "installation" })}>
          Flownodes <span>{installation.name}</span>
        </button>
        <nav className="modes" aria-label="Mode">
          {modes
            .filter((m) => m.show)
            .map((m) => (
              <button key={m.key} aria-current={mode === m.key && !choosing} onClick={() => (setChoosing(false), nav.go(m.first))}>
                {m.label}
                {m.key === "operate" && unacked > 0 && (
                  <span className="mode-alarm" aria-label={`${unacked} unacknowledged Alarms`}>
                    {unacked}
                  </span>
                )}
              </button>
            ))}
        </nav>
        <span className="grow" />
        <button className="search" onClick={() => setPalette(true)}>
          Go to… <kbd>Ctrl K</kbd>
        </button>
        <span className="who">{access.persona.name}</span>
      </header>
      <div className="frame">
        <aside className="navigator" aria-label="Navigator">
          {mode !== "installation" && (
            <button className="ns-switch" onClick={() => setChoosing(true)}>
              <span className="dim">Namespace</span>
              <span className="ident">{access.namespace}</span>
            </button>
          )}
          {mode === "operate" && access.canRead && (
            <>
              <NavGroup items={["alarms"]} nav={nav} counts={{ alarms: unacked }} />
              <h2 className="nav-heading">Assets</h2>
              <Tree nodes={tree} nav={nav} depth={0} />
            </>
          )}
          {mode === "configure" && access.canRead && (
            <>
              <NavGroup items={configureSections} nav={nav} counts={counts} />
              {access.canEngineer && <NavGroup items={["apply"]} nav={nav} />}
            </>
          )}
          {mode === "installation" && <NavGroup items={access.canAdminister ? installationSections : ["installation"]} nav={nav} />}
        </aside>
        <main className="workspace">{main}</main>
        {mode === "operate" && access.canRead && !choosing && nav.place.section !== "alarms" && (
          <aside className="rail" aria-label="Alarms in this Namespace">
            <h2>Alarms</h2>
            {list.filter((a) => !a.acknowledged || a.active).length === 0 && <p className="empty">Nothing abnormal.</p>}
            {list
              .filter((a) => !a.acknowledged || a.active)
              .sort((x, y) => Number(x.acknowledged) - Number(y.acknowledged) || rank[y.priority] - rank[x.priority])
              .map((a) => (
                <button key={a.id} className={`rail-alarm p-${a.priority} ${a.acknowledged ? "acked" : ""}`} onClick={() => nav.go({ section: "assets", asset: a.asset })}>
                  <PriorityGlyph alarm={a} />
                  <span>
                    <strong>{a.message}</strong>
                    <span className="ident dim">{a.asset}</span>
                    <span className="dim">
                      {a.active ? "Active" : "Cleared"}, {a.acknowledged ? "acknowledged" : "unacknowledged"}, since {a.raised}
                    </span>
                  </span>
                </button>
              ))}
          </aside>
        )}
      </div>
      <RecentChanges />
      {palette && <Palette nav={nav} close={() => setPalette(false)} setNamespace={setNamespace} />}
    </div>
  );
}
