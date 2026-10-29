// PROTOTYPE (#104): who can do what, and how the UI shows what they can't.
// Assumption: any Namespace role reads everything in it (#80); writes follow the glossary.
import { createContext, useContext, type ReactNode } from "react";
import { namespaces, type Persona } from "./data";

export type Gating = "hide" | "disable";

export interface Access {
  persona: Persona;
  namespace: string | null;
  gating: Gating;
  canRead: boolean;
  canOperate: boolean;
  canEngineer: boolean;
  canAdminister: boolean;
  visibleNamespaces: string[];
}

export function computeAccess(persona: Persona, namespace: string | null, gating: Gating): Access {
  const roles = namespace ? (persona.grants[namespace] ?? []) : [];
  return {
    persona,
    namespace,
    gating,
    canRead: roles.length > 0,
    canOperate: roles.includes("Operator"),
    canEngineer: roles.includes("Engineer"),
    canAdminister: persona.administrator,
    visibleNamespaces: persona.administrator ? namespaces : namespaces.filter((n) => persona.grants[n]),
  };
}

export const AccessContext = createContext<Access>(null!);
export const useAccess = () => useContext(AccessContext);

// A control the current persona may lack the right for. In "hide" mode it vanishes;
// in "disable" mode it stays, greyed, with the missing role in its tooltip.
export function Gate({ allowed, need, children }: { allowed: boolean; need: string; children: ReactNode }) {
  const { gating } = useAccess();
  if (allowed) return <>{children}</>;
  if (gating === "hide") return null;
  return (
    <span className="gated" title={`Needs the ${need} role`}>
      {children}
    </span>
  );
}
