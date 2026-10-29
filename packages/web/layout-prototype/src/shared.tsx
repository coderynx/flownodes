// PROTOTYPE (#104): content panels every variant reuses. Layout and navigation are
// what the variants disagree about, so they live in the variant files, not here.
import { createContext, useContext, useState } from "react";
import { Gate, useAccess } from "./access";
import {
  assetTrees,
  discoveries,
  findAsset,
  namespaces,
  resources,
  users,
  type Alarm,
  type AssetNode,
  type Quality,
  type ResourceRow,
} from "./data";

export type Section =
  | "home"
  | "assets"
  | "alarms"
  | "flows"
  | "devices"
  | "connections"
  | "assetTypes"
  | "secrets"
  | "apply"
  | "installation"
  | "admin-namespaces"
  | "admin-users";

export interface Place {
  section: Section;
  asset?: string;
  resource?: { kind: string; name: string };
}

export interface Nav {
  place: Place;
  go: (place: Place) => void;
}

export const sectionLabels: Record<Section, string> = {
  home: "Overview",
  assets: "Assets",
  alarms: "Alarms",
  flows: "Flows",
  devices: "Devices",
  connections: "Connections",
  assetTypes: "Asset Types",
  secrets: "Secrets",
  apply: "Apply Manifest",
  installation: "Summary",
  "admin-namespaces": "Namespaces",
  "admin-users": "Users and Role Grants",
};

export const sectionKind: Partial<Record<Section, string>> = {
  flows: "Flow",
  devices: "Device",
  connections: "Connection",
  assetTypes: "AssetType",
  secrets: "Secret",
};

export const kindSection: Record<string, Section> = {
  Flow: "flows",
  Device: "devices",
  Connection: "connections",
  AssetType: "assetTypes",
  Secret: "secrets",
};

// Alarms are the one piece of live state the prototype mutates, so acknowledging
// one moves every variant's badge.
export const AlarmsContext = createContext<{ list: Alarm[]; acknowledge: (id: string) => void }>(null!);
export const useAlarms = () => useContext(AlarmsContext);

export function openAlarmCount(list: Alarm[]) {
  return list.filter((a) => !a.acknowledged).length;
}

export function QualityBadge({ q }: { q: Quality }) {
  return <span className={`badge q-${q}`}>{q}</span>;
}

export function PriorityBadge({ p }: { p: Alarm["priority"] }) {
  return <span className={`badge p-${p}`}>{p}</span>;
}

export function DataPointTable({ asset }: { asset: AssetNode }) {
  const { canOperate } = useAccess();
  const [writing, setWriting] = useState<string | null>(null);
  if (asset.dataPoints.length === 0) return <p className="muted">No Data Points.</p>;
  return (
    <table>
      <thead>
        <tr>
          <th>Data Point</th>
          <th>Value</th>
          <th>Quality</th>
          <th>Timestamp</th>
          <th>Source</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {asset.dataPoints.map((d) => (
          <tr key={d.name}>
            <td className="mono">{d.name}</td>
            <td className="value">
              {writing === d.name ? (
                <input autoFocus defaultValue={d.value} size={6} onBlur={() => setWriting(null)} />
              ) : (
                <>
                  {d.value} {d.unit}
                </>
              )}
            </td>
            <td>
              <QualityBadge q={d.quality} />
            </td>
            <td className="mono muted">{d.at}</td>
            <td className="mono muted small">{d.source}</td>
            <td>
              {d.writable && (
                <Gate allowed={canOperate} need="Operator">
                  <button disabled={!canOperate} onClick={() => setWriting(d.name)}>
                    Write
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

export function ActionList({ asset }: { asset: AssetNode }) {
  const { canOperate } = useAccess();
  if (asset.actions.length === 0) return <p className="muted">No Actions.</p>;
  return (
    <div className="row">
      {asset.actions.map((a) => (
        <Gate key={a} allowed={canOperate} need="Operator">
          <button disabled={!canOperate}>▶ {a}</button>
        </Gate>
      ))}
    </div>
  );
}

export function AlarmTable({ asset, compact, nav }: { asset?: string; compact?: boolean; nav: Nav }) {
  const { canOperate } = useAccess();
  const { list, acknowledge } = useAlarms();
  const rows = asset ? list.filter((a) => a.asset === asset) : list;
  if (rows.length === 0) return <p className="muted">No Alarms.</p>;
  return (
    <table className={compact ? "compact" : undefined}>
      <thead>
        <tr>
          <th>Priority</th>
          <th>Alarm</th>
          {!asset && <th>Asset</th>}
          <th>State</th>
          {!compact && <th>Raised</th>}
          <th />
        </tr>
      </thead>
      <tbody>
        {rows.map((a) => (
          <tr key={a.id} className={a.acknowledged ? "muted" : undefined}>
            <td>
              <PriorityBadge p={a.priority} />
            </td>
            <td>{a.message}</td>
            {!asset && (
              <td>
                <a onClick={() => nav.go({ section: "assets", asset: a.asset })}>{a.asset}</a>
              </td>
            )}
            <td className="small">
              {a.active ? "active" : "cleared"} · {a.acknowledged ? "acknowledged" : "unacknowledged"}
            </td>
            {!compact && <td className="mono muted">{a.raised}</td>}
            <td>
              {!a.acknowledged && (
                <Gate allowed={canOperate} need="Operator">
                  <button disabled={!canOperate} onClick={() => acknowledge(a.id)}>
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

export function EngineerButton({ label, onClick }: { label: string; onClick?: () => void }) {
  const { canEngineer } = useAccess();
  return (
    <Gate allowed={canEngineer} need="Engineer">
      <button className="primary" disabled={!canEngineer} onClick={onClick}>
        {label}
      </button>
    </Gate>
  );
}

export function AssetDetail({ path, nav }: { path: AssetNode[]; nav: Nav }) {
  const asset = path[path.length - 1];
  return (
    <div className="stack">
      <div className="row spread">
        <div>
          <h2>{asset.title}</h2>
          <div className="muted small">
            <span className="mono">Asset/{asset.name}</span>
            {asset.type ? (
              <>
                {" "}
                · instance of{" "}
                <a onClick={() => nav.go({ section: "assetTypes", resource: { kind: "AssetType", name: asset.type! } })}>
                  {asset.type}
                </a>
              </>
            ) : (
              " · ad hoc"
            )}
          </div>
        </div>
        <div className="row">
          <button>Revisions</button>
          <EngineerButton label="Edit Manifest" />
        </div>
      </div>
      <section>
        <h3>Data Points</h3>
        <DataPointTable asset={asset} />
      </section>
      <section>
        <h3>Actions</h3>
        <ActionList asset={asset} />
      </section>
      <section>
        <h3>Alarms</h3>
        <AlarmTable asset={asset.name} compact nav={nav} />
      </section>
      <section>
        <h3>Devices attached</h3>
        {asset.devices.length === 0 ? (
          <p className="muted">None.</p>
        ) : (
          <div className="row">
            {asset.devices.map((d) => (
              <a key={d} className="chip" onClick={() => nav.go({ section: "devices", resource: { kind: "Device", name: d } })}>
                {d}
              </a>
            ))}
            <EngineerButton label="Attach Device" />
          </div>
        )}
      </section>
      {asset.children.length > 0 && (
        <section>
          <h3>Child Assets</h3>
          <div className="row">
            {asset.children.map((c) => (
              <a key={c.name} className="chip" onClick={() => nav.go({ section: "assets", asset: c.name })}>
                {c.title}
              </a>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}

export function useNamespaceData() {
  const { namespace } = useAccess();
  const ns = namespace ?? "default";
  return { tree: assetTrees[ns], rows: resources[ns], discoveries: discoveries[ns] };
}

export function useAssetPath(name?: string) {
  const { tree } = useNamespaceData();
  return name ? findAsset(tree, name) : null;
}

export function ResourceTable({ kind, nav }: { kind: string; nav: Nav }) {
  const { rows } = useNamespaceData();
  const list = rows.filter((r) => r.kind === kind);
  return (
    <table>
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
              <a className="mono" onClick={() => nav.go({ section: kindSection[kind], resource: { kind, name: r.name } })}>
                {r.name}
              </a>
            </td>
            <td>{r.status}</td>
            <td className="mono">
              {r.generation}
              {r.reconciled < r.generation && <span className="warn"> (reconciling {r.reconciled})</span>}
            </td>
            <td className="muted small">{r.detail}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

export function ResourceList({ kind, nav }: { kind: string; nav: Nav }) {
  const { discoveries: found } = useNamespaceData();
  return (
    <div className="stack">
      <div className="row spread">
        <h2>{sectionLabels[kindSection[kind]]}</h2>
        <EngineerButton label={`New ${kind}`} onClick={() => nav.go({ section: "apply" })} />
      </div>
      <ResourceTable kind={kind} nav={nav} />
      {kind === "Device" && (
        <section>
          <h3>Discoveries</h3>
          {found.length === 0 ? (
            <p className="muted">Nothing undeclared.</p>
          ) : (
            <ul>
              {found.map((d) => (
                <li key={d}>
                  {d} <EngineerButton label="Declare Device" />
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </div>
  );
}

function fakeYaml(r: ResourceRow, namespace: string) {
  const body =
    r.kind === "Secret"
      ? "  values:\n    username: <write-only>\n    password: <write-only>"
      : r.kind === "Flow"
        ? `  executionMode: ${r.detail.split(" · ")[1]}\n  nodes:\n    - id: trigger\n      ...`
        : `  # ${r.detail}`;
  return `apiVersion: flownodes/v1\nkind: ${r.kind}\nmetadata:\n  name: ${r.name}\n  namespace: ${namespace}\n  generation: ${r.generation}\nspec:\n${body}`;
}

export function ResourceDetail({ kind, name, nav }: { kind: string; name: string; nav: Nav }) {
  const { namespace, canEngineer, canOperate } = useAccess();
  const { rows } = useNamespaceData();
  const r = rows.find((x) => x.kind === kind && x.name === name);
  if (!r) return <p className="muted">Not found in this Namespace.</p>;
  return (
    <div className="stack">
      <div className="row spread">
        <div>
          <h2 className="mono">
            {kind}/{name}
          </h2>
          <div className="muted small">
            {r.status} · Generation {r.generation}, reconciled {r.reconciled}
          </div>
        </div>
        <div className="row">
          <button>Revisions</button>
          <EngineerButton label="Edit Manifest" />
          <Gate allowed={canEngineer} need="Engineer">
            <button className="danger" disabled={!canEngineer}>
              Delete
            </button>
          </Gate>
        </div>
      </div>
      {kind === "Flow" && (
        <section>
          <h3>Graph (read-only)</h3>
          <div className="graph">[ trigger ] → [ condition ] → [ action call ] — drawn by @xyflow/react + dagre</div>
          <h3>Recent Flow Executions</h3>
          <p className="muted small">#4182 completed 13:50 · #4181 completed 13:40 · …</p>
        </section>
      )}
      {kind === "Connection" && (
        <section>
          <h3>Actions and Events</h3>
          <Gate allowed={canOperate} need="Operator">
            <button disabled={!canOperate}>▶ pair</button>
          </Gate>
        </section>
      )}
      <section>
        <h3>Manifest</h3>
        <pre className="yaml">{fakeYaml(r, namespace ?? "default")}</pre>
        {!canEngineer && <p className="muted small">Read-only: editing needs the Engineer role.</p>}
      </section>
      <a onClick={() => nav.go({ section: kindSection[kind] })}>← all {sectionLabels[kindSection[kind]]}</a>
    </div>
  );
}

export function ApplyPage() {
  const { canEngineer } = useAccess();
  return (
    <div className="stack">
      <h2>Apply Manifest</h2>
      <p className="muted small">CodeMirror 6 editor goes here. Validation in the editor is advisory; Dry run calls ?dryRun=true.</p>
      <textarea className="yaml" rows={14} defaultValue={"apiVersion: flownodes/v1\nkind: Asset\nmetadata:\n  name: train-103\nspec:\n  type: Train\n"} readOnly={!canEngineer} />
      <div className="row">
        <EngineerButton label="Dry run" />
        <EngineerButton label="Apply" />
      </div>
    </div>
  );
}

export function AdminNamespaces() {
  return (
    <div className="stack">
      <div className="row spread">
        <h2>Namespaces</h2>
        <button className="primary">New Namespace</button>
      </div>
      <table>
        <thead>
          <tr>
            <th>Namespace</th>
            <th>Users with Role Grants</th>
          </tr>
        </thead>
        <tbody>
          {namespaces.map((n) => (
            <tr key={n}>
              <td className="mono">{n}</td>
              <td className="muted small">{users.filter((u) => u.grants.includes(n)).map((u) => u.name).join(", ") || "—"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function AdminUsers() {
  return (
    <div className="stack">
      <div className="row spread">
        <h2>Users and Role Grants</h2>
        <button className="primary">New User</button>
      </div>
      <table>
        <thead>
          <tr>
            <th>User</th>
            <th>Role Grants</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {users.map((u) => (
            <tr key={u.name} className={u.disabled ? "muted" : undefined}>
              <td className="mono">
                {u.name}
                {u.disabled && " (disabled)"}
              </td>
              <td className="small">{u.grants}</td>
              <td>
                <button>Edit grants</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function NoAccess() {
  const { namespace, persona } = useAccess();
  return (
    <div className="stack">
      <h2>No Role Grant in {namespace}</h2>
      <p className="muted">
        {persona.name} is an Administrator but holds no Viewer, Operator or Engineer role here, so the Namespace's
        Resources and live state stay hidden. Grant yourself a role under Users and Role Grants to see them.
      </p>
    </div>
  );
}

// Renders the main content for a place. Variants decide what surrounds it.
export function Content({ nav }: { nav: Nav }) {
  const { canRead } = useAccess();
  const { place } = nav;
  const path = useAssetPath(place.asset);
  if (place.section === "admin-namespaces") return <AdminNamespaces />;
  if (place.section === "admin-users") return <AdminUsers />;
  if (!canRead) return <NoAccess />;
  if (place.section === "assets" && path) return <AssetDetail path={path} nav={nav} />;
  if (place.section === "alarms")
    return (
      <div className="stack">
        <h2>Alarms</h2>
        <AlarmTable nav={nav} />
      </div>
    );
  if (place.section === "apply") return <ApplyPage />;
  if (place.resource) return <ResourceDetail kind={place.resource.kind} name={place.resource.name} nav={nav} />;
  const kind = sectionKind[place.section];
  if (kind) return <ResourceList kind={kind} nav={nav} />;
  return <p className="muted">Pick an Asset.</p>;
}
