# PROTOTYPE: Operator Alarm view (#105)

Throwaway. This answers "how does an Operator see and handle Alarms?" inside the #104 shell (its Variant C) and is not the start of `Web.Client`.

```sh
cd packages/web/alarm-prototype && npm install && npm run dev
```

The pink pieces are the prototype's controls, not part of the design:

- The bar at the bottom: `←` / `→` (or the arrow keys) switch variants (`?variant=A|B|C`), and the selects switch persona and gating.
  - **A, Summary table**: one dense list with History as a tab. Rows expand in place. The list holds still while your pointer is on it.
  - **B, Queue**: lanes on the left, one Alarm's full story on the right. Acknowledging moves on to the next.
  - **C, By Asset**: Alarms grouped by train or machine, beside a chronological logbook built from the history.
- The panel at the bottom left plays the Engine: raise, storm, clear, active again, another Operator acknowledging, retire, a Priority change and a dropped stream. Its log shows the events the stream would send.

The data is in memory. `src/store.ts` copies the lifecycle from `Alarm.cs`, the query from `GET .../alarms/history`, and the stream from ADR-0032.
