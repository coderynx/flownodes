// PROTOTYPE (#105): three Alarm views inside the #104 shell, switchable with ?variant=A|B|C,
// with a persona picker and a simulator that plays the Engine's side of the live stream.
// Throwaway: nothing here ships.
import { StrictMode, useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { AccessContext, computeAccess, type Gating } from "./access";
import { AlarmVariantContext, alarmVariants, type AlarmVariant } from "./AlarmViews";
import { personas, type Persona } from "./data";
import { AlarmsContext, sectionLabels, type Place } from "./shared";
import { acknowledge as acknowledgeAlarm, byUrgency, simulate, useStore } from "./store";
import { VariantC } from "./VariantC";
import "./styles.css";

const keys = Object.keys(alarmVariants) as AlarmVariant[];

function param(name: string, fallback: string) {
  return new URLSearchParams(location.search).get(name) ?? fallback;
}

function setParam(name: string, value: string) {
  const url = new URL(location.href);
  url.searchParams.set(name, value);
  history.replaceState(null, "", url);
}

function firstNamespace(p: Persona) {
  return Object.keys(p.grants)[0] ?? "default";
}

function landing(p: Persona, ns: string): Place {
  return (p.grants[ns] ?? []).length === 0 ? { section: "installation" } : { section: "alarms" };
}

function Simulator({ namespace }: { namespace: string }) {
  const store = useStore();
  const [open, setOpen] = useState(true);
  const actions: [string, () => void][] = [
    ["Raise one", () => simulate.raise(namespace)],
    ["Storm (+15)", () => simulate.raise(namespace, 15)],
    ["Clear one", () => simulate.clear(namespace)],
    ["Active again", () => simulate.reactivate(namespace)],
    ["Ben acknowledges", () => simulate.otherAcknowledges(namespace)],
    ["Retire one", () => simulate.retire(namespace)],
    ["Priority goes up", () => simulate.escalate(namespace)],
    ["Drop stream (6 s)", () => simulate.dropStream()],
  ];
  return (
    <div className="sim" aria-label="Prototype simulator">
      <div className="sim-head">
        <span>Simulated Engine ({store.stream})</span>
        <button onClick={() => setOpen(!open)}>{open ? "Hide" : "Show"}</button>
      </div>
      {open && (
        <>
          <div className="sim-buttons">
            {actions.map(([label, run]) => (
              <button key={label} onClick={run}>
                {label}
              </button>
            ))}
            <button style={{ gridColumn: "1 / -1" }} onClick={() => simulate.toggleAuto(namespace)}>
              {store.auto ? "Stop auto" : "Auto: a change every 3 s"}
            </button>
          </div>
          <pre aria-label="Events on the stream">{store.log.join("\n")}</pre>
        </>
      )}
    </div>
  );
}

function App() {
  const [variant, setVariant] = useState<AlarmVariant>(param("variant", "A") as AlarmVariant);
  const [persona, setPersona] = useState(personas.find((p) => p.key === param("persona", "ada")) ?? personas[0]);
  const [gating, setGating] = useState<Gating>(param("gating", "hide") as Gating);
  const [namespace, setNamespaceState] = useState(firstNamespace(persona));
  const [place, setPlace] = useState<Place>(landing(persona, namespace));
  const store = useStore();

  AlarmVariantContext.current = variant;
  const access = computeAccess(persona, namespace, gating);
  const nav = { place, go: setPlace };
  const setNamespace = (ns: string) => {
    setNamespaceState(ns);
    setPlace(landing(persona, ns));
  };

  const cycle = (step: number) => {
    const next = keys[(keys.indexOf(variant) + step + keys.length) % keys.length];
    setVariant(next);
    setParam("variant", next);
  };

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const t = e.target as HTMLElement;
      if (t.closest("input, textarea, select, [contenteditable]")) return;
      if (e.key === "ArrowLeft") cycle(-1);
      if (e.key === "ArrowRight") cycle(1);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  });

  const list = [...(store.live[namespace] ?? [])].sort(byUrgency);
  return (
    <AccessContext.Provider value={access}>
      <AlarmsContext.Provider value={{ list, acknowledge: (id, comment) => acknowledgeAlarm(namespace, id, persona.name, comment) }}>
        <VariantC nav={nav} setNamespace={setNamespace} />
        {import.meta.env.DEV && (
          <>
            <Simulator namespace={namespace} />
            <div className="proto-bar">
              <button onClick={() => cycle(-1)}>←</button>
              <b>
                {variant} ({alarmVariants[variant].name})
              </b>
              <button onClick={() => cycle(1)}>→</button>
              <span className="sep" />
              <select
                value={persona.key}
                onChange={(e) => {
                  const p = personas.find((x) => x.key === e.target.value)!;
                  const ns = firstNamespace(p);
                  setPersona(p);
                  setNamespaceState(ns);
                  setPlace(landing(p, ns));
                  setParam("persona", p.key);
                }}
              >
                {personas.map((p) => (
                  <option key={p.key} value={p.key}>
                    {p.name}: {p.blurb}
                  </option>
                ))}
              </select>
              <select
                value={gating}
                onChange={(e) => {
                  setGating(e.target.value as Gating);
                  setParam("gating", e.target.value);
                }}
              >
                <option value="hide">Hide what the role can't do</option>
                <option value="disable">Show it disabled</option>
              </select>
              <span className="sep" />
              <span className="proto-state">
                ns={namespace} · {(persona.grants[namespace] ?? []).join("+") || "no role"} · {list.length} live · {sectionLabels[place.section]}
                {place.asset && `/${place.asset}`}
              </span>
            </div>
          </>
        )}
      </AlarmsContext.Provider>
    </AccessContext.Provider>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
