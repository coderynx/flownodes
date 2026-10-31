// PROTOTYPE (#105): three ways an Operator could see and handle Alarms, mounted on the Alarms
// page of the #104 shell and switched with ?variant=A|B|C. Throwaway: nothing here ships.
//   A, Summary table: one dense list, the classic alarm summary, with History as a tab.
//   B, Queue: work through Alarms one at a time, with the full story beside the list.
//   C, By Asset: where the trouble is, grouped by machine, beside a chronological logbook.
import { useEffect, useRef, useState, type ReactNode } from "react";
import { Gate, useAccess } from "./access";
import { assetTrees, findAsset, type Alarm, type Priority } from "./data";
import { acknowledge, byUrgency, clock, dismissNotice, findAlarm, history, priorityRank, since, stamp, useStore } from "./store";
import { PriorityGlyph } from "./VariantC";
import type { Nav } from "./shared";
import "./alarm-views.css";

const priorities: Priority[] = ["critical", "high", "medium", "low"];

function useNow() {
  const [now, setNow] = useState(Date.now());
  useEffect(() => {
    const t = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(t);
  }, []);
  return now;
}

function useLive() {
  const { namespace } = useAccess();
  const store = useStore();
  return { ns: namespace!, list: [...(store.live[namespace!] ?? [])].sort(byUrgency), store };
}

// New Alarms don't move rows under a pointer that is about to click one. The rows keep their
// order while frozen; what arrived meanwhile is `held`, and the view must show it anyway.
function useStableOrder<T extends { id: string }>(list: T[], frozen: boolean) {
  const order = useRef<string[]>([]);
  if (!frozen) order.current = list.map((x) => x.id);
  const byId = new Map(list.map((x) => [x.id, x]));
  return {
    rows: order.current.map((id) => byId.get(id)).filter((x): x is T => x !== undefined),
    held: list.filter((x) => !order.current.includes(x.id)),
  };
}

// Keyboard handling works as soon as the page opens, without a click into it first.
function useFocusOnOpen() {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => ref.current?.focus({ preventScroll: true }), []);
  return ref;
}

function stateWord(a: Alarm) {
  if (a.state === "ended") return "Ended";
  if (a.state === "retired") return "Retired";
  return a.active ? "Active" : "Cleared";
}

function StateCell({ alarm }: { alarm: Alarm }) {
  return (
    <>
      {stateWord(alarm)}
      {alarm.state !== "retired" && alarm.state !== "ended" && <span className="dim">, {alarm.acknowledged ? "acknowledged" : "unacknowledged"}</span>}
      {alarm.activeCount > 1 && <span className="flap" title={`Became active ${alarm.activeCount} times`}> ×{alarm.activeCount}</span>}
    </>
  );
}

function AssetLink({ name, nav }: { name: string; nav: Nav }) {
  const { namespace } = useAccess();
  const path = findAsset(assetTrees[namespace!] ?? [], name) ?? [];
  return (
    <button className="link asset-link" onClick={() => nav.go({ section: "assets", asset: name })} title={name}>
      {path.length ? path.map((n) => n.title).join(" › ") : name}
    </button>
  );
}

function PriorityCounts({ list }: { list: Alarm[] }) {
  return (
    <div className="counts" aria-label="Unacknowledged Alarms by Alarm Priority">
      {priorities.map((p) => {
        const n = list.filter((a) => a.priority === p && !a.acknowledged).length;
        return (
          <span key={p} className={n ? "pcount" : "pcount none"}>
            <PriorityGlyph alarm={{ priority: p, acknowledged: n === 0 }} /> {n} {p}
          </span>
        );
      })}
    </div>
  );
}

// Without this an Operator can't tell a quiet plant from a dead connection.
function Banners() {
  const { store } = useLive();
  const now = useNow();
  return (
    <>
      {store.stream !== "live" && store.lostAt && (
        <p className="banner lost" role="alert">
          <strong>Not live.</strong> The connection to the Engine dropped {since(store.lostAt, now)} ago, so this list shows the state at {clock(store.lostAt)}. Reconnecting…
        </p>
      )}
      {store.notice && (
        <p className="banner notice" role="status">
          {store.notice} <button className="link" onClick={dismissNotice}>Dismiss</button>
        </p>
      )}
    </>
  );
}

function AckButton({ alarm, comment, primary, label = "Acknowledge", onDone }: { alarm: Alarm; comment?: string; primary?: boolean; label?: string; onDone?: () => void }) {
  const { canAcknowledge, persona } = useAccess();
  const { ns, store } = useLive();
  if (alarm.acknowledged || alarm.endedAt !== null) return null;
  const pending = store.pending.includes(alarm.id);
  return (
    <Gate allowed={canAcknowledge} need="Operator">
      <button
        className={primary ? "btn primary" : "btn"}
        disabled={!canAcknowledge || pending || store.stream !== "live"}
        onClick={(e) => {
          e.stopPropagation();
          acknowledge(ns, alarm.id, persona.name, comment);
          onDone?.();
        }}
      >
        {pending ? "Acknowledging…" : label}
      </button>
    </Gate>
  );
}

// An Alarm's story, from the fields AlarmDto has. It keeps the latest clear and a count of
// activations, so the times of earlier activations can't be shown.
function Timeline({ alarm }: { alarm: Alarm }) {
  const steps: { at: number; text: ReactNode }[] = [{ at: alarm.raisedAt, text: "Raised" }];
  if (alarm.acknowledgedAt !== null)
    steps.push({
      at: alarm.acknowledgedAt,
      text: (
        <>
          Acknowledged by {alarm.acknowledgedBy}
          {alarm.acknowledgementComment && <q>{alarm.acknowledgementComment}</q>}
        </>
      ),
    });
  if (alarm.clearedAt !== null) steps.push({ at: alarm.clearedAt, text: "Cleared" });
  if (alarm.endedAt !== null && alarm.state === "ended") steps.push({ at: alarm.endedAt, text: "Ended" });
  if (alarm.endedAt !== null && alarm.state === "retired") steps.push({ at: alarm.endedAt, text: "Retired: its Alarm Condition changed or went away" });
  steps.sort((x, y) => x.at - y.at);
  return (
    <ol className="timeline">
      {steps.map((s, i) => (
        <li key={i}>
          <span className="num dim">{stamp(s.at)}</span> {s.text}
        </li>
      ))}
      {alarm.activeCount > 1 && <li className="dim">Became active {alarm.activeCount} times in all; the earlier times aren't kept.</li>}
    </ol>
  );
}

function CommentAck({ alarm, primary, label, onDone }: { alarm: Alarm; primary?: boolean; label?: string; onDone?: () => void }) {
  const [comment, setComment] = useState("");
  const { canAcknowledge } = useAccess();
  if (alarm.acknowledged || alarm.endedAt !== null) return null;
  return (
    <div className="comment-ack">
      {canAcknowledge && (
        <input aria-label="Comment on the acknowledgement" placeholder="Comment (optional)" value={comment} onChange={(e) => setComment(e.target.value)} />
      )}
      <AckButton alarm={alarm} comment={comment} primary={primary} label={label} onDone={onDone} />
    </div>
  );
}

// The query GET .../alarms/history supports, and no more.
function useHistory(pageSize: number) {
  const { ns, store } = useLive();
  const [asset, setAsset] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [offset, setOffset] = useState(0);
  const page = history(ns, {
    asset: asset || undefined,
    from: from ? new Date(from).getTime() : undefined,
    to: to ? new Date(to).getTime() : undefined,
    offset,
    limit: pageSize,
  });
  const assets = [...new Set(history(ns, { offset: 0, limit: 1000 }).items.map((a) => a.asset))].sort();
  void store;
  const filters = (
    <div className="history-filters">
      <label>
        Asset
        <select value={asset} onChange={(e) => (setAsset(e.target.value), setOffset(0))}>
          <option value="">Any</option>
          {assets.map((a) => (
            <option key={a}>{a}</option>
          ))}
        </select>
      </label>
      <label>
        Raised from <input type="date" value={from} onChange={(e) => (setFrom(e.target.value), setOffset(0))} />
      </label>
      <label>
        to <input type="date" value={to} onChange={(e) => (setTo(e.target.value), setOffset(0))} />
      </label>
    </div>
  );
  return { page, filters, offset, setOffset, pageSize };
}

function Pager({ total, offset, pageSize, setOffset }: { total: number; offset: number; pageSize: number; setOffset: (n: number) => void }) {
  return (
    <div className="pager">
      <span className="dim">
        {total === 0 ? "No Alarms" : `${offset + 1}–${Math.min(offset + pageSize, total)} of ${total}`}. Kept for 30 days.
      </span>
      <button className="btn" disabled={offset === 0} onClick={() => setOffset(Math.max(0, offset - pageSize))}>Newer</button>
      <button className="btn" disabled={offset + pageSize >= total} onClick={() => setOffset(offset + pageSize)}>Older</button>
    </div>
  );
}

function PageHead({ list, children }: { list: Alarm[]; children?: ReactNode }) {
  const { namespace } = useAccess();
  return (
    <div className="object-head">
      <div>
        <p className="object-kind">Namespace {namespace}</p>
        <h1>Alarms</h1>
      </div>
      <PriorityCounts list={list} />
      {children}
    </div>
  );
}

// ---------------------------------------------------------------------- A, Summary table

function HeldBar({ held, release }: { held: Alarm[]; release: () => void }) {
  if (held.length === 0) return null;
  const worst = [...held].sort(byUrgency)[0];
  return (
    <button className={`held p-${worst.priority}`} onClick={release}>
      <PriorityGlyph alarm={worst} />
      <span>
        <strong>{held.length} new while the list is held:</strong> {worst.message} on {worst.asset}. Move the pointer away or press to show.
      </span>
    </button>
  );
}

function VariantASummary({ nav }: { nav: Nav }) {
  const { ns, list, store } = useLive();
  const { canAcknowledge, persona } = useAccess();
  const [tab, setTab] = useState<"live" | "history">("live");
  const [onlyUnacked, setOnlyUnacked] = useState(false);
  const [open, setOpen] = useState<string | null>(null);
  const [picked, setPicked] = useState<string[]>([]);
  const [hovering, setHovering] = useState(false);
  const [cursor, setCursor] = useState(0);
  const now = useNow();
  const hist = useHistory(15);
  const focusRef = useFocusOnOpen();

  const shown = onlyUnacked ? list.filter((a) => !a.acknowledged) : list;
  const frozen = hovering || open !== null || picked.length > 0;
  const { rows, held } = useStableOrder(shown, frozen);
  const pickable = picked.filter((id) => rows.some((r) => r.id === id && !r.acknowledged));

  const onKey = (e: React.KeyboardEvent) => {
    if ((e.target as HTMLElement).closest("input, select, textarea")) return;
    const here = rows[cursor];
    if (e.key === "j") setCursor(Math.min(cursor + 1, rows.length - 1));
    else if (e.key === "k") setCursor(Math.max(cursor - 1, 0));
    else if (e.key === "Enter" && here) setOpen(open === here.id ? null : here.id);
    else if (e.key === "a" && here && canAcknowledge && !here.acknowledged) acknowledge(ns, here.id, persona.name);
  };

  return (
    <div ref={focusRef} className="focus-root" onKeyDown={onKey} tabIndex={-1}>
      <PageHead list={list} />
      <div className="toolbar">
        {tab === "live" && (
          <>
            <label className="check">
              <input type="checkbox" checked={onlyUnacked} onChange={(e) => setOnlyUnacked(e.target.checked)} /> Unacknowledged only
            </label>
            <span className="tool-sep" />
            <Gate allowed={canAcknowledge} need="Operator">
              <button
                className="btn"
                disabled={!canAcknowledge || pickable.length === 0 || store.stream !== "live"}
                onClick={() => {
                  pickable.forEach((id) => acknowledge(ns, id, persona.name));
                  setPicked([]);
                }}
              >
                Acknowledge selected ({pickable.length})
              </button>
            </Gate>
          </>
        )}
        <span className="grow" />
        <span className="dim keys">j / k move · Enter opens · a acknowledges</span>
      </div>
      <div className="tabs" role="tablist">
        <button role="tab" aria-selected={tab === "live"} onClick={() => setTab("live")}>Live ({list.length})</button>
        <button role="tab" aria-selected={tab === "history"} onClick={() => setTab("history")}>History</button>
      </div>
      <div className="object-body">
        <Banners />
        {tab === "live" && (
          <>
            <HeldBar held={held} release={() => { setHovering(false); setOpen(null); setPicked([]); }} />
            {rows.length === 0 ? (
              <p className="empty">Nothing abnormal in this Namespace.</p>
            ) : (
              <table className={store.stream === "live" ? "dense" : "dense stale"} onMouseEnter={() => setHovering(true)} onMouseLeave={() => setHovering(false)}>
                <thead>
                  <tr>
                    <th />
                    <th>Priority</th>
                    <th>Alarm</th>
                    <th>Asset</th>
                    <th>State</th>
                    <th>Raised</th>
                    <th>For</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {rows.map((a, i) => (
                    <SummaryRow
                      key={a.id}
                      alarm={a}
                      nav={nav}
                      now={now}
                      open={open === a.id}
                      atCursor={i === cursor}
                      picked={picked.includes(a.id)}
                      toggle={() => setOpen(open === a.id ? null : a.id)}
                      pick={(on) => setPicked(on ? [...picked, a.id] : picked.filter((x) => x !== a.id))}
                    />
                  ))}
                </tbody>
              </table>
            )}
          </>
        )}
        {tab === "history" && (
          <>
            {hist.filters}
            <table className="dense">
              <thead>
                <tr>
                  <th>Priority</th>
                  <th>Alarm</th>
                  <th>Asset</th>
                  <th>Raised</th>
                  <th>Cleared</th>
                  <th>Acknowledged</th>
                  <th>State</th>
                </tr>
              </thead>
              <tbody>
                {hist.page.items.map((a) => (
                  <tr key={a.id} className={a.endedAt !== null ? "quiet" : undefined}>
                    <td><span className="prio"><PriorityGlyph alarm={{ priority: a.priority, acknowledged: true }} /> {a.priority}</span></td>
                    <td>{a.message}{a.acknowledgementComment && <div className="dim small-note">“{a.acknowledgementComment}”</div>}</td>
                    <td><AssetLink name={a.asset} nav={nav} /></td>
                    <td className="num">{stamp(a.raisedAt)}</td>
                    <td className="num">{a.clearedAt ? stamp(a.clearedAt) : "–"}</td>
                    <td>{a.acknowledgedAt ? `${a.acknowledgedBy}, ${stamp(a.acknowledgedAt)}` : "–"}</td>
                    <td><StateCell alarm={a} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
            <Pager total={hist.page.total} offset={hist.offset} pageSize={hist.pageSize} setOffset={hist.setOffset} />
          </>
        )}
      </div>
    </div>
  );
}

function SummaryRow(p: { alarm: Alarm; nav: Nav; now: number; open: boolean; atCursor: boolean; picked: boolean; toggle: () => void; pick: (on: boolean) => void }) {
  const { alarm: a, nav, now } = p;
  const { canAcknowledge } = useAccess();
  return (
    <>
      <tr className={`${a.acknowledged ? "quiet" : ""} ${p.atCursor ? "cursor" : ""} clickable`} onClick={p.toggle}>
        <td onClick={(e) => e.stopPropagation()}>
          {canAcknowledge && !a.acknowledged && (
            <input type="checkbox" aria-label={`Select ${a.message}`} checked={p.picked} onChange={(e) => p.pick(e.target.checked)} />
          )}
        </td>
        <td><span className="prio"><PriorityGlyph alarm={a} /> {a.priority}</span></td>
        <td>{a.message}</td>
        <td onClick={(e) => e.stopPropagation()}><AssetLink name={a.asset} nav={nav} /></td>
        <td><StateCell alarm={a} /></td>
        <td className="num">{a.raised}</td>
        <td className="num">{since(a.raisedAt, a.clearedAt ?? now)}</td>
        <td className="end" onClick={(e) => e.stopPropagation()}><AckButton alarm={a} /></td>
      </tr>
      {p.open && (
        <tr className="expanded">
          <td />
          <td colSpan={7}>
            <div className="expanded-body">
              <div>
                <h2>What happened</h2>
                <Timeline alarm={a} />
                <p className="dim">Alarm Condition <span className="ident">{a.condition}</span></p>
              </div>
              <div>
                <h2>Do</h2>
                <CommentAck alarm={a} />
                <button className="btn" onClick={() => nav.go({ section: "assets", asset: a.asset })}>Open Asset</button>
              </div>
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

// ---------------------------------------------------------------------- B, Queue

function QueueItem({ a, selected, onPick, now }: { a: Alarm; selected: boolean; onPick: () => void; now: number }) {
  return (
    <li>
      <button className={`q-item ${selected ? "selected" : ""} ${a.acknowledged ? "acked" : ""}`} onClick={onPick} aria-current={selected}>
        <PriorityGlyph alarm={a} />
        <span className="q-text">
          <strong>{a.message}</strong>
          <span className="dim ident">{a.asset}</span>
        </span>
        <span className="dim num q-age">
          {since(a.raisedAt, now)}
          {a.activeCount > 1 && <span className="flap"> ×{a.activeCount}</span>}
        </span>
      </button>
    </li>
  );
}

function VariantBQueue({ nav }: { nav: Nav }) {
  const { ns, list, store } = useLive();
  const { canAcknowledge, persona } = useAccess();
  const [mode, setMode] = useState<"live" | "history">("live");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const now = useNow();
  const hist = useHistory(25);
  const focusRef = useFocusOnOpen();

  const lanes = [
    { title: "Active, not acknowledged", items: list.filter((a) => a.active && !a.acknowledged) },
    { title: "Cleared, not acknowledged", items: list.filter((a) => !a.active && !a.acknowledged) },
    { title: "Acknowledged, still active", items: list.filter((a) => a.acknowledged) },
  ];
  const ordered = lanes.flatMap((l) => l.items);
  const items = mode === "live" ? ordered : hist.page.items;
  const selected = (selectedId && findAlarm(ns, selectedId)) || items[0];
  const waiting = list.filter((a) => !a.acknowledged);
  const moreUrgent = selected ? waiting.find((a) => a.id !== selected.id && priorityRank[a.priority] > priorityRank[selected.priority]) : undefined;

  const nextAfter = (id: string) => {
    const unacked = ordered.filter((a) => !a.acknowledged);
    const at = unacked.findIndex((a) => a.id === id);
    return unacked[at + 1]?.id ?? unacked.find((a) => a.id !== id)?.id ?? null;
  };

  const onKey = (e: React.KeyboardEvent) => {
    if ((e.target as HTMLElement).closest("input, select, textarea")) return;
    const at = items.findIndex((a) => a.id === selected?.id);
    if (e.key === "j" && items[at + 1]) setSelectedId(items[at + 1].id);
    if (e.key === "k" && items[at - 1]) setSelectedId(items[at - 1].id);
    if (e.key === "a" && selected && canAcknowledge && !selected.acknowledged && selected.endedAt === null) {
      acknowledge(ns, selected.id, persona.name);
      setSelectedId(nextAfter(selected.id));
    }
  };

  return (
    <div ref={focusRef} className="queue focus-root" onKeyDown={onKey} tabIndex={-1}>
      <div className="q-list">
        <div className="q-top">
          <h1>Alarms</h1>
          <PriorityCounts list={list} />
          <div className="seg" role="tablist">
            <button role="tab" aria-selected={mode === "live"} onClick={() => setMode("live")}>Live ({list.length})</button>
            <button role="tab" aria-selected={mode === "history"} onClick={() => setMode("history")}>History</button>
          </div>
        </div>
        {mode === "live" ? (
          <div className={store.stream === "live" ? undefined : "stale"}>
            {list.length === 0 && <p className="empty pad">Nothing abnormal in this Namespace.</p>}
            {lanes.map(
              (lane) =>
                lane.items.length > 0 && (
                  <section key={lane.title}>
                    <h2 className="lane">{lane.title} ({lane.items.length})</h2>
                    <ul>
                      {lane.items.map((a) => (
                        <QueueItem key={a.id} a={a} selected={selected?.id === a.id} onPick={() => setSelectedId(a.id)} now={now} />
                      ))}
                    </ul>
                  </section>
                ),
            )}
          </div>
        ) : (
          <div>
            <div className="pad">{hist.filters}</div>
            <ul>
              {hist.page.items.map((a) => (
                <li key={a.id}>
                  <button className={`q-item acked ${selected?.id === a.id ? "selected" : ""}`} onClick={() => setSelectedId(a.id)}>
                    <PriorityGlyph alarm={{ priority: a.priority, acknowledged: true }} />
                    <span className="q-text">
                      <strong>{a.message}</strong>
                      <span className="dim ident">{a.asset}</span>
                    </span>
                    <span className="dim num q-age">{stamp(a.raisedAt)}</span>
                  </button>
                </li>
              ))}
            </ul>
            <div className="pad"><Pager total={hist.page.total} offset={hist.offset} pageSize={hist.pageSize} setOffset={hist.setOffset} /></div>
          </div>
        )}
      </div>
      <div className="q-detail">
        <Banners />
        {moreUrgent && mode === "live" && (
          <button className={`held p-${moreUrgent.priority}`} onClick={() => setSelectedId(moreUrgent.id)}>
            <PriorityGlyph alarm={moreUrgent} />
            <span><strong>More urgent:</strong> {moreUrgent.message} on {moreUrgent.asset}. Go to it.</span>
          </button>
        )}
        {!selected ? (
          <p className="empty">Nothing to handle. New Alarms appear here as they are raised.</p>
        ) : (
          <article key={selected.id}>
            <p className="object-kind"><span className="prio"><PriorityGlyph alarm={selected} size={18} /> {selected.priority} Alarm</span></p>
            <h1>{selected.message}</h1>
            <p className="q-asset"><AssetLink name={selected.asset} nav={nav} /></p>
            <p className="q-state"><StateCell alarm={selected} /></p>
            {selected.endedAt !== null && mode === "live" && (
              <p className="banner notice">
                {selected.state === "retired" ? "This Alarm was retired because its Alarm Condition changed or went away. It needs nothing from you." : "This Alarm has ended."}
              </p>
            )}
            <div className="q-actions">
              <CommentAck alarm={selected} primary label="Acknowledge and next" onDone={() => setSelectedId(nextAfter(selected.id))} />
              <button className="btn" onClick={() => nav.go({ section: "assets", asset: selected.asset })}>Open Asset</button>
            </div>
            <h2>What happened</h2>
            <Timeline alarm={selected} />
            <dl className="facts">
              <dt>Alarm Condition</dt>
              <dd className="ident">{selected.condition}</dd>
              <dt>{selected.active ? "Active for" : "Lasted"}</dt>
              <dd className="num">{since(selected.raisedAt, selected.clearedAt ?? now)}</dd>
            </dl>
            <p className="dim keys">j / k move · a acknowledges and moves on</p>
          </article>
        )}
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------- C, By Asset

function VariantCByAsset({ nav }: { nav: Nav }) {
  const { ns, list, store } = useLive();
  const { canAcknowledge, persona } = useAccess();
  const [closed, setClosed] = useState<string[]>([]);
  const [scope, setScope] = useState<string | null>(null);
  const [hovering, setHovering] = useState(false);
  const now = useNow();
  const tree = assetTrees[ns] ?? [];

  // A group is the Asset just under the tree's top level: the train or the machine.
  const groupOf = (asset: string) => {
    const path = findAsset(tree, asset) ?? [];
    return path[1] ?? path[0];
  };
  const groups = [...new Set(list.map((a) => groupOf(a.asset)?.name ?? a.asset))]
    .map((id) => {
      const alarms = list.filter((a) => (groupOf(a.asset)?.name ?? a.asset) === id);
      return { id, alarms, title: groupOf(alarms[0].asset)?.title ?? id };
    })
    .sort((x, y) => byUrgency(x.alarms[0], y.alarms[0]));
  const { rows, held } = useStableOrder(groups, hovering);
  const shown = [...rows, ...held];

  // A logbook of what happened, built from history pages. The history filters on one exact
  // Asset name, so scoping to a group filters the page on the client.
  const [pages, setPages] = useState(1);
  const page = history(ns, { offset: 0, limit: 30 * pages });
  const inScope = (a: Alarm) => !scope || (groupOf(a.asset)?.name ?? a.asset) === scope;
  const entries = page.items
    .filter(inScope)
    .flatMap((a) => [
      { at: a.raisedAt, what: "Raised", a },
      ...(a.acknowledgedAt !== null ? [{ at: a.acknowledgedAt, what: `Acknowledged by ${a.acknowledgedBy}`, a }] : []),
      ...(a.clearedAt !== null ? [{ at: a.clearedAt, what: a.state === "ended" && a.endedAt === a.clearedAt ? "Cleared and ended" : "Cleared", a }] : []),
      ...(a.state === "retired" && a.endedAt !== null ? [{ at: a.endedAt, what: "Retired", a }] : []),
      ...(a.state === "ended" && a.endedAt !== null && a.endedAt !== a.clearedAt ? [{ at: a.endedAt, what: "Ended", a }] : []),
    ])
    .sort((x, y) => y.at - x.at);
  const days = [...new Set(entries.map((e) => new Date(e.at).toDateString()))];

  return (
    <div className="by-asset">
      <div className="ba-main">
        <PageHead list={list} />
        <div className="ba-body" onMouseEnter={() => setHovering(true)} onMouseLeave={() => setHovering(false)}>
          <Banners />
          {shown.length === 0 && <p className="empty">Nothing abnormal in this Namespace.</p>}
          <div className={store.stream === "live" ? undefined : "stale"}>
            {shown.map((g) => {
              const unacked = g.alarms.filter((a) => !a.acknowledged);
              const isOpen = !closed.includes(g.id);
              return (
                <section key={g.id} className={`ba-group ${scope === g.id ? "scoped" : ""}`}>
                  <header>
                    <button className="ba-toggle" aria-expanded={isOpen} onClick={() => setClosed(isOpen ? [...closed, g.id] : closed.filter((c) => c !== g.id))}>
                      {isOpen ? "▾" : "▸"}
                    </button>
                    <PriorityGlyph alarm={g.alarms[0]} size={16} />
                    <button className="ba-title" onClick={() => nav.go({ section: "assets", asset: g.id })}>{g.title}</button>
                    <span className="dim">{unacked.length} to acknowledge · {g.alarms.length} live</span>
                    <span className="grow" />
                    <button className="link" onClick={() => setScope(scope === g.id ? null : g.id)}>{scope === g.id ? "Whole logbook" : "Logbook"}</button>
                    {unacked.length > 1 && (
                      <Gate allowed={canAcknowledge} need="Operator">
                        <button
                          className="btn"
                          disabled={!canAcknowledge || store.stream !== "live"}
                          onClick={() => unacked.forEach((a) => acknowledge(ns, a.id, persona.name))}
                        >
                          Acknowledge all {unacked.length}
                        </button>
                      </Gate>
                    )}
                  </header>
                  {isOpen && (
                    <ul>
                      {g.alarms.map((a) => {
                        const leaf = (findAsset(tree, a.asset) ?? []).slice(2).map((n) => n.title).join(" › ");
                        return (
                          <li key={a.id} className={a.acknowledged ? "ba-row quiet" : "ba-row"}>
                            <PriorityGlyph alarm={a} />
                            <span className="ba-where">
                              {leaf ? (
                                <button className="link" onClick={() => nav.go({ section: "assets", asset: a.asset })}>{leaf}</button>
                              ) : (
                                <span className="dim">This Asset</span>
                              )}
                            </span>
                            <span className="ba-message">{a.message}</span>
                            <span className="ba-state"><StateCell alarm={a} /></span>
                            <span className="num dim ba-age">{since(a.raisedAt, a.clearedAt ?? now)}</span>
                            <span className="ba-act"><AckButton alarm={a} /></span>
                          </li>
                        );
                      })}
                    </ul>
                  )}
                </section>
              );
            })}
          </div>
        </div>
      </div>
      <aside className="logbook" aria-label="Logbook">
        <h2>Logbook{scope && <span className="dim"> · {scope}</span>}</h2>
        {days.map((day) => (
          <section key={day}>
            <h3>{new Date(day).toLocaleDateString("en-GB", { weekday: "short", day: "2-digit", month: "short" })}</h3>
            <ul>
              {entries
                .filter((e) => new Date(e.at).toDateString() === day)
                .map((e, i) => (
                  <li key={`${e.a.id}-${e.what}-${i}`}>
                    <span className="num dim">{clock(e.at)}</span>
                    <span>
                      <strong>{e.what}</strong> {e.a.message}
                      <button className="link ident log-asset" onClick={() => nav.go({ section: "assets", asset: e.a.asset })}>{e.a.asset}</button>
                      {e.what.startsWith("Acknowledged") && e.a.acknowledgementComment && <q className="dim">{e.a.acknowledgementComment}</q>}
                    </span>
                  </li>
                ))}
            </ul>
          </section>
        ))}
        {page.total > page.items.length && <button className="btn" onClick={() => setPages(pages + 1)}>Older</button>}
      </aside>
    </div>
  );
}

// ----------------------------------------------------------------------

export const alarmVariants = {
  A: { name: "Summary table", View: VariantASummary },
  B: { name: "Queue", View: VariantBQueue },
  C: { name: "By Asset, with a logbook", View: VariantCByAsset },
} as const;

export type AlarmVariant = keyof typeof alarmVariants;

export const AlarmVariantContext = { current: "A" as AlarmVariant };

export function AlarmsPage({ nav }: { nav: Nav }) {
  const { View } = alarmVariants[AlarmVariantContext.current];
  return <View nav={nav} />;
}
