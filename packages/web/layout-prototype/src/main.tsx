// PROTOTYPE (#104): three layouts for the web UI, switchable with ?variant=A|B|C, and a
// persona picker that shows what each role sees. Throwaway: nothing here ships.
import { StrictMode, useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { AccessContext, computeAccess, type Gating } from "./access";
import { alarms as initialAlarms, assetTrees, personas, type Persona } from "./data";
import { AlarmsContext, sectionLabels, type Place } from "./shared";
import * as A from "./VariantA";
import * as B from "./VariantB";
import * as C from "./VariantC";
import "./styles.css";

const variants = { A, B, C } as const;
type Key = keyof typeof variants;
const keys = Object.keys(variants) as Key[];

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

// Where a persona lands: the Engineer-only lands on configuration, anyone who
// operates lands on the Assets, an Administrator with no grants lands on Users.
function landing(p: Persona, ns: string, variant: Key): Place {
  const roles = p.grants[ns] ?? [];
  if (roles.length === 0) return { section: variant === "C" ? "installation" : p.administrator ? "admin-users" : "home" };
  if (variant === "B") return { section: "home" };
  if (variant === "C" && roles.includes("Engineer") && !roles.includes("Operator")) return { section: "flows" };
  return { section: "assets", asset: assetTrees[ns][0]?.name };
}

function App() {
  const [variant, setVariant] = useState<Key>(param("variant", "A") as Key);
  const [persona, setPersona] = useState(personas.find((p) => p.key === param("persona", "cara")) ?? personas[0]);
  const [gating, setGating] = useState<Gating>(param("gating", "hide") as Gating);
  const [namespace, setNamespaceState] = useState(firstNamespace(persona));
  const [place, setPlace] = useState<Place>(landing(persona, namespace, variant));
  const [alarms, setAlarms] = useState(initialAlarms);

  const access = computeAccess(persona, namespace, gating);
  const nav = { place, go: setPlace };
  const setNamespace = (ns: string) => {
    setNamespaceState(ns);
    setPlace(landing(persona, ns, variant));
  };
  const acknowledge = (id: string) =>
    setAlarms({ ...alarms, [namespace]: alarms[namespace].map((a) => (a.id === id ? { ...a, acknowledged: true } : a)) });

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

  const V = variants[variant];
  const View = (V as { [k: string]: unknown })[`Variant${variant}`] as typeof A.VariantA;
  return (
    <AccessContext.Provider value={access}>
      <AlarmsContext.Provider value={{ list: alarms[namespace], acknowledge }}>
        <View nav={nav} setNamespace={setNamespace} />
        {import.meta.env.DEV && (
          <div className="proto-bar">
            <button onClick={() => cycle(-1)}>←</button>
            <b>
              {variant} ({V.name})
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
                setPlace(landing(p, ns, variant));
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
              ns={namespace} · roles={(persona.grants[namespace] ?? []).join("+") || "none"}
              {persona.administrator && "+Admin"} · {sectionLabels[place.section]}
              {place.asset && `/${place.asset}`}
              {place.resource && `/${place.resource.kind}/${place.resource.name}`}
            </span>
          </div>
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
