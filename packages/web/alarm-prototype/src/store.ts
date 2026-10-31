// PROTOTYPE (#105): a fake Engine and a fake live stream, in memory. The lifecycle copies
// Engine.Domain/Alarms/Alarm.cs, the history query copies GET .../alarms/history, and the stream
// follows ADR-0032: `alarm` events, then on a reconnect `reset`, a fresh snapshot and `ready`.
// Assumption the ADR leaves open: an Alarm that ends sends a last `alarm` event with its final
// state, rather than a bare `removed`, so a client can say why it left.
import { useSyncExternalStore } from "react";
import { assetTrees, type Alarm, type AlarmState, type AssetNode, type Priority } from "./data";

type Row = Omit<Alarm, "active" | "acknowledged" | "raised">;

const MINUTE = 60_000;
const DAY = 24 * 60 * MINUTE;

export function clock(ms: number) {
  return new Date(ms).toLocaleTimeString("en-GB");
}

export function stamp(ms: number) {
  const d = new Date(ms);
  return `${d.toLocaleDateString("en-GB", { day: "2-digit", month: "short" })} ${clock(ms)}`;
}

export function since(ms: number, now: number) {
  const s = Math.max(0, Math.round((now - ms) / 1000));
  if (s < 60) return `${s} s`;
  if (s < 3600) return `${Math.floor(s / 60)} min`;
  if (s < 86400) return `${Math.floor(s / 3600)} h ${Math.floor((s % 3600) / 60)} min`;
  return `${Math.floor(s / 86400)} d`;
}

const view = (r: Row): Alarm => ({
  ...r,
  active: r.state === "active",
  acknowledged: r.acknowledgedAt !== null,
  raised: clock(r.raisedAt),
});

let nextId = 1;
function row(asset: string, condition: string, priority: Priority, message: string, raisedAt: number, patch: Partial<Row> = {}): Row {
  return {
    id: `a${nextId++}`,
    asset,
    condition,
    priority,
    message,
    state: "active",
    raisedAt,
    activeCount: 1,
    acknowledgedAt: null,
    acknowledgedBy: null,
    acknowledgementComment: null,
    clearedAt: null,
    endedAt: null,
    ...patch,
  };
}

// What can go wrong in the sample Namespaces, by Asset name suffix.
const catalogue: { match: RegExp; condition: string; priority: Priority; message: string }[] = [
  { match: /brakes$/, condition: "lowPressure", priority: "critical", message: "Brake pressure below 3 bar for 10 s" },
  { match: /brakes$/, condition: "pressureDrift", priority: "medium", message: "Brake pressure off setpoint by 0.5 bar" },
  { match: /doors$/, condition: "doorStuck", priority: "high", message: "Door open for more than 30 s" },
  { match: /car-\d$/, condition: "cabinHot", priority: "medium", message: "Cabin temperature above 26 °C" },
  { match: /car-\d$/, condition: "occupancyStale", priority: "low", message: "Occupancy reading older than 5 min" },
  { match: /^train-\d+$/, condition: "gpsLost", priority: "low", message: "No GPS fix for 2 min" },
  { match: /wash-bay/, condition: "lowWater", priority: "low", message: "Water level below 70 %" },
  { match: /wash-bay/, condition: "pumpDry", priority: "high", message: "Pump running with no flow" },
  { match: /press-1/, condition: "oilHot", priority: "high", message: "Oil temperature above 70 °C" },
  { match: /press-1/, condition: "guardOpen", priority: "critical", message: "Guard open while cycling" },
];

const flatten = (nodes: AssetNode[]): AssetNode[] => nodes.flatMap((n) => [n, ...flatten(n.children)]);
const pick = <T,>(items: T[]): T => items[Math.floor(Math.random() * items.length)];

function randomRow(ns: string, raisedAt: number, table: Record<string, Row[]> = tableRef()): Row | null {
  const options = flatten(assetTrees[ns] ?? []).flatMap((node) =>
    catalogue
      .filter((c) => c.match.test(node.name))
      // An Alarm Condition holds at most one live Alarm on an Asset.
      .filter((c) => !table[ns]?.some((r) => r.endedAt === null && r.asset === node.name && r.condition === c.condition))
      .map((c) => ({ node, c })),
  );
  if (options.length === 0) return null;
  const { node, c } = pick(options);
  return row(node.name, c.condition, c.priority, c.message, raisedAt);
}

function seed(): Record<string, Row[]> {
  const now = Date.now();
  const ago = (m: number) => now - m * MINUTE;
  const depot: Row[] = [
    row("train-101-car-1-brakes", "lowPressure", "critical", "Brake pressure below 3 bar for 10 s", ago(3)),
    row("train-102-car-1-doors", "doorStuck", "high", "Door open for more than 30 s", ago(26), { activeCount: 4 }),
    row("train-102-car-2", "cabinHot", "medium", "Cabin temperature above 26 °C", ago(170), { state: "cleared", clearedAt: ago(41) }),
    row("wash-bay", "lowWater", "low", "Water level below 70 %", ago(190), {
      acknowledgedAt: ago(170),
      acknowledgedBy: "Ben",
      acknowledgementComment: "Tank refill booked for tonight",
    }),
    row("train-101-car-2", "occupancyStale", "low", "Occupancy reading older than 5 min", ago(52), { state: "cleared", clearedAt: ago(8) }),
  ];
  // Finished Alarms for the history, spread over the 30 days AlarmRetention keeps.
  for (let i = 0; i < 46; i++) {
    const raisedAt = now - (0.2 + i * 0.63) * DAY;
    const old = randomRow("rail-depot", raisedAt, {})!;
    const clearedAt = raisedAt + (5 + i * 3) * MINUTE;
    if (i % 9 === 4) depot.push({ ...old, state: "retired", endedAt: clearedAt, clearedAt });
    else
      depot.push({
        ...old,
        state: "ended",
        activeCount: i % 7 === 0 ? 3 : 1,
        acknowledgedAt: clearedAt - 2 * MINUTE,
        acknowledgedBy: pick(["Ada", "Ben", "Cara"]),
        acknowledgementComment: i % 5 === 0 ? "Checked on site, no fault found" : null,
        clearedAt,
        endedAt: clearedAt,
      });
  }
  return {
    "rail-depot": depot,
    "plant-north": [row("press-1", "oilHot", "high", "Oil temperature above 70 °C", ago(14))],
    default: [],
  };
}

const table = seed();
function tableRef() {
  return table;
}
const isLive = (r: Row) => r.endedAt === null;
const rank: Record<Priority, number> = { critical: 4, high: 3, medium: 2, low: 1 };

export type StreamState = "live" | "reconnecting";

export interface StoreState {
  live: Record<string, Alarm[]>;
  stream: StreamState;
  lostAt: number | null;
  log: string[];
  pending: string[];
  notice: string | null;
  auto: boolean;
}

let state: StoreState = {
  live: Object.fromEntries(Object.entries(table).map(([ns, rows]) => [ns, rows.filter(isLive).map(view)])),
  stream: "live",
  lostAt: null,
  log: [`${clock(Date.now())} ready (snapshot sent)`],
  pending: [],
  notice: null,
  auto: false,
};

const listeners = new Set<() => void>();
function set(patch: Partial<StoreState>) {
  state = { ...state, ...patch };
  listeners.forEach((l) => l());
}

function logged(line: string) {
  return [`${clock(Date.now())} ${line}`, ...state.log].slice(0, 14);
}

export function useStore() {
  return useSyncExternalStore(
    (l) => {
      listeners.add(l);
      return () => listeners.delete(l);
    },
    () => state,
  );
}

// The Engine changed an Alarm. The client only hears about it while its stream is up.
function save(ns: string, r: Row, why: string) {
  const rows = table[ns];
  const at = rows.findIndex((x) => x.id === r.id);
  if (at >= 0) rows[at] = r;
  else rows.unshift(r);
  if (state.stream !== "live") {
    set({ log: logged(`(not delivered: ${why} ${r.asset})`) });
    return;
  }
  const others = state.live[ns].filter((x) => x.id !== r.id);
  set({
    live: { ...state.live, [ns]: isLive(r) ? [...others, view(r)] : others },
    log: logged(`alarm ${r.state}${r.acknowledgedAt ? "+ack" : ""} ${r.asset} (${why})`),
  });
}

const stateOf = (r: Row, outcome?: AlarmState): AlarmState => outcome ?? (r.clearedAt !== null ? "cleared" : "active");

function clearRow(ns: string, r: Row) {
  const now = Date.now();
  const cleared: Row = { ...r, clearedAt: now };
  save(ns, r.acknowledgedAt !== null ? { ...cleared, state: "ended", endedAt: now } : { ...cleared, state: "cleared" }, "cleared");
}

function acknowledgeRow(ns: string, r: Row, by: string, comment: string | null) {
  const now = Date.now();
  const acked: Row = { ...r, acknowledgedAt: now, acknowledgedBy: by, acknowledgementComment: comment };
  save(ns, r.clearedAt !== null ? { ...acked, state: "ended", endedAt: now } : acked, `acknowledged by ${by}`);
}

// POST .../alarms/{id}/acknowledge, with a little latency so the pending state shows. The
// row changes when the stream says so, never optimistically.
export function acknowledge(ns: string, id: string, by: string, comment?: string) {
  set({ pending: [...state.pending, id], notice: null });
  setTimeout(() => {
    const pending = state.pending.filter((x) => x !== id);
    const r = table[ns].find((x) => x.id === id);
    if (state.stream !== "live") {
      set({ pending, notice: "Not acknowledged: the connection to the Engine is down. Try again once it is back." });
    } else if (!r || !isLive(r)) {
      set({ pending, notice: "That Alarm has already ended, so there is nothing left to acknowledge." });
    } else if (r.acknowledgedAt !== null) {
      set({ pending, notice: `${r.acknowledgedBy} had already acknowledged that Alarm. Your comment was not kept.` });
    } else {
      set({ pending });
      acknowledgeRow(ns, r, by, comment?.trim() || null);
    }
  }, 500);
}

export function dismissNotice() {
  set({ notice: null });
}

// GET .../alarms/history: newest first, by Asset (an exact name, not a subtree) and raised
// time, and it includes live Alarms. No filter by Alarm Priority, state or text exists.
export function history(ns: string, q: { asset?: string; from?: number; to?: number; offset: number; limit: number }) {
  const rows = table[ns]
    .filter((r) => (!q.asset || r.asset === q.asset) && (q.from === undefined || r.raisedAt >= q.from) && (q.to === undefined || r.raisedAt < q.to))
    .sort((a, b) => b.raisedAt - a.raisedAt);
  return { items: rows.slice(q.offset, q.offset + q.limit).map(view), total: rows.length };
}

export function findAlarm(ns: string, id: string) {
  const r = table[ns].find((x) => x.id === id);
  return r ? view(r) : undefined;
}

// Unacknowledged first, then worst Alarm Priority, then newest.
export function byUrgency(a: Alarm, b: Alarm) {
  return Number(a.acknowledged) - Number(b.acknowledged) || rank[b.priority] - rank[a.priority] || b.raisedAt - a.raisedAt;
}

export const priorityRank = rank;

// Simulator: what the outside world does to this Namespace.
const liveRows = (ns: string) => table[ns].filter(isLive);

export const simulate = {
  raise(ns: string, count = 1) {
    for (let i = 0; i < count; i++) {
      const r = randomRow(ns, Date.now());
      if (r) save(ns, r, "raised");
    }
  },
  clear(ns: string) {
    const options = liveRows(ns).filter((r) => r.clearedAt === null);
    if (options.length) clearRow(ns, pick(options));
  },
  reactivate(ns: string) {
    const options = liveRows(ns).filter((r) => r.clearedAt !== null && r.acknowledgedAt === null);
    if (options.length) {
      const r = pick(options);
      const next: Row = { ...r, clearedAt: null, activeCount: r.activeCount + 1 };
      save(ns, { ...next, state: stateOf(next) }, "active again");
    }
  },
  otherAcknowledges(ns: string) {
    const options = liveRows(ns).filter((r) => r.acknowledgedAt === null);
    if (options.length) acknowledgeRow(ns, pick(options), "Ben", null);
  },
  retire(ns: string) {
    const options = liveRows(ns);
    if (options.length) {
      const now = Date.now();
      save(ns, { ...pick(options), state: "retired", endedAt: now }, "Alarm Condition changed");
    }
  },
  escalate(ns: string) {
    const options = liveRows(ns).filter((r) => r.priority !== "critical");
    if (options.length) {
      const r = pick(options);
      const up: Priority = r.priority === "low" ? "medium" : r.priority === "medium" ? "high" : "critical";
      save(ns, { ...r, priority: up }, `Alarm Condition now ${up}`);
    }
  },
  // The stream drops; the Engine carries on; after 6 s the client gets reset, snapshot, ready.
  dropStream() {
    if (state.stream !== "live") return;
    set({ stream: "reconnecting", lostAt: Date.now(), log: logged("stream closed, reconnecting") });
    setTimeout(() => {
      const count = Object.values(table).reduce((n, rows) => n + rows.filter(isLive).length, 0);
      set({
        stream: "live",
        lostAt: null,
        live: Object.fromEntries(Object.entries(table).map(([ns, rows]) => [ns, rows.filter(isLive).map(view)])),
        log: logged(`reset, ${count} alarm (snapshot), ready`),
      });
    }, 6000);
  },
  toggleAuto(ns: string) {
    set({ auto: !state.auto });
    autoNamespace = ns;
  },
};

let autoNamespace = "rail-depot";
setInterval(() => {
  if (!state.auto) return;
  const steps = [simulate.raise, simulate.clear, simulate.reactivate, simulate.otherAcknowledges, simulate.clear];
  pick(steps)(autoNamespace);
}, 3000);
