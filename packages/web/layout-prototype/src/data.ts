// PROTOTYPE (#104): in-memory mock data. Nothing here talks to the Web API.

export type Role = "Viewer" | "Operator" | "Engineer";
export type Quality = "good" | "uncertain" | "bad";
export type Priority = "low" | "medium" | "high" | "critical";

export interface Persona {
  key: string;
  name: string;
  administrator: boolean;
  grants: Record<string, Role[]>;
  blurb: string;
}

export const personas: Persona[] = [
  { key: "dan", name: "Dan", administrator: false, grants: { "rail-depot": ["Viewer"] }, blurb: "Viewer in rail-depot" },
  {
    key: "ada",
    name: "Ada",
    administrator: false,
    grants: { "rail-depot": ["Operator"], "plant-north": ["Operator"] },
    blurb: "Operator in rail-depot and plant-north",
  },
  {
    key: "ben",
    name: "Ben",
    administrator: false,
    grants: { "rail-depot": ["Engineer"], "plant-north": ["Viewer"] },
    blurb: "Engineer in rail-depot, Viewer in plant-north",
  },
  {
    key: "cara",
    name: "Cara",
    administrator: false,
    grants: { "rail-depot": ["Operator", "Engineer"] },
    blurb: "Operator + Engineer in rail-depot",
  },
  { key: "eve", name: "Eve", administrator: true, grants: {}, blurb: "Administrator, no Namespace grants" },
  {
    key: "fay",
    name: "Fay",
    administrator: true,
    grants: { default: ["Engineer", "Operator"], "rail-depot": ["Engineer"] },
    blurb: "Administrator + Engineer/Operator in default, Engineer in rail-depot",
  },
];

export const namespaces = ["default", "rail-depot", "plant-north"];

export interface DataPoint {
  name: string;
  value: string;
  unit?: string;
  quality: Quality;
  at: string;
  writable: boolean;
  source: string;
}

export interface AssetNode {
  name: string;
  title: string;
  type?: string;
  children: AssetNode[];
  dataPoints: DataPoint[];
  actions: string[];
  devices: string[];
}

const dp = (
  name: string,
  value: string,
  unit: string | undefined,
  quality: Quality,
  writable: boolean,
  source: string,
): DataPoint => ({ name, value, unit, quality, at: "14:02:17", writable, source });

const car = (train: string, n: number, brake: string, q: Quality): AssetNode => ({
  name: `${train}-car-${n}`,
  title: `Car ${n}`,
  type: "PassengerCar",
  children: [
    {
      name: `${train}-car-${n}-doors`,
      title: "Doors",
      type: "DoorSet",
      children: [],
      dataPoints: [dp("open", "false", undefined, "good", false, `device/${train}-c${n}-door-plc.open`)],
      actions: ["openDoors", "closeDoors"],
      devices: [`${train}-c${n}-door-plc`],
    },
    {
      name: `${train}-car-${n}-brakes`,
      title: "Brakes",
      type: "BrakeUnit",
      children: [],
      dataPoints: [
        dp("pressure", brake, "bar", q, false, `device/${train}-c${n}-brake.pressure`),
        dp("pressureSetpoint", "5.0", "bar", "good", true, `device/${train}-c${n}-brake.setpoint`),
      ],
      actions: ["releaseBrakes"],
      devices: [`${train}-c${n}-brake`],
    },
  ],
  dataPoints: [
    dp("temperature", "21.4", "°C", "good", false, `device/${train}-c${n}-hvac.temp`),
    dp("temperatureSetpoint", "21.0", "°C", "good", true, `device/${train}-c${n}-hvac.setpoint`),
    dp("occupancy", "34", "%", "uncertain", false, "computed"),
  ],
  actions: [],
  devices: [`${train}-c${n}-hvac`],
});

const train = (name: string, title: string, brake: string, q: Quality): AssetNode => ({
  name,
  title,
  type: "Train",
  children: [car(name, 1, brake, q), car(name, 2, "5.1", "good")],
  dataPoints: [
    dp("speed", "0", "km/h", "good", false, `device/${name}-tcms.speed`),
    dp("position", "Depot track 4", undefined, "good", false, `device/${name}-gps.position`),
    dp("inService", "true", undefined, "good", true, "flow/service-schedule"),
  ],
  actions: ["wakeUp", "sleep"],
  devices: [`${name}-tcms`, `${name}-gps`],
});

export const assetTrees: Record<string, AssetNode[]> = {
  "rail-depot": [
    {
      name: "fleet",
      title: "Fleet",
      children: [train("train-101", "Train 101", "2.7", "bad"), train("train-102", "Train 102", "5.0", "good")],
      dataPoints: [dp("trainsInService", "1", undefined, "good", false, "computed")],
      actions: [],
      devices: [],
    },
    {
      name: "depot",
      title: "Depot",
      children: [
        {
          name: "wash-bay",
          title: "Wash bay",
          children: [],
          dataPoints: [
            dp("waterLevel", "68", "%", "good", false, "device/wash-tank.level"),
            dp("pumpOn", "false", undefined, "good", true, "device/wash-pump.on"),
          ],
          actions: ["startWashCycle"],
          devices: ["wash-tank", "wash-pump"],
        },
      ],
      dataPoints: [dp("outdoorTemperature", "9.8", "°C", "uncertain", false, "flow/weather-to-depot")],
      actions: [],
      devices: [],
    },
  ],
  "plant-north": [
    {
      name: "line-1",
      title: "Line 1",
      children: [
        {
          name: "press-1",
          title: "Press 1",
          type: "HydraulicPress",
          children: [],
          dataPoints: [
            dp("oilTemperature", "71", "°C", "good", false, "device/press-1-plc.oil"),
            dp("cycleCount", "18234", undefined, "good", false, "device/press-1-plc.cycles"),
          ],
          actions: ["stop"],
          devices: ["press-1-plc"],
        },
      ],
      dataPoints: [dp("throughput", "412", "parts/h", "good", false, "computed")],
      actions: [],
      devices: [],
    },
  ],
  default: [
    {
      name: "home",
      title: "Home",
      children: [
        {
          name: "living-room",
          title: "Living room",
          children: [],
          dataPoints: [dp("lights", "on", undefined, "good", true, "device/hue-living.on")],
          actions: [],
          devices: ["hue-living"],
        },
      ],
      dataPoints: [],
      actions: [],
      devices: [],
    },
  ],
};

export interface Alarm {
  id: string;
  asset: string;
  message: string;
  priority: Priority;
  active: boolean;
  acknowledged: boolean;
  raised: string;
}

export const alarms: Record<string, Alarm[]> = {
  "rail-depot": [
    {
      id: "a1",
      asset: "train-101-car-1-brakes",
      message: "Brake pressure below 3 bar for 10 s",
      priority: "critical",
      active: true,
      acknowledged: false,
      raised: "14:01:55",
    },
    {
      id: "a2",
      asset: "wash-bay",
      message: "Water level below 70 %",
      priority: "low",
      active: true,
      acknowledged: true,
      raised: "11:20:03",
    },
    {
      id: "a3",
      asset: "train-102-car-2",
      message: "Cabin temperature above 26 °C",
      priority: "medium",
      active: false,
      acknowledged: false,
      raised: "09:12:40",
    },
  ],
  "plant-north": [
    {
      id: "b1",
      asset: "press-1",
      message: "Oil temperature above 70 °C",
      priority: "high",
      active: true,
      acknowledged: false,
      raised: "13:58:10",
    },
  ],
  default: [],
};

export interface ResourceRow {
  kind: string;
  name: string;
  generation: number;
  reconciled: number;
  status: string;
  detail: string;
}

export const resources: Record<string, ResourceRow[]> = {
  "rail-depot": [
    { kind: "Connection", name: "depot-mqtt", generation: 3, reconciled: 3, status: "Connected", detail: "mqtt · broker.depot.local:8883" },
    { kind: "Connection", name: "tcms-modbus", generation: 7, reconciled: 7, status: "Connected", detail: "modbus-tcp · 4 Devices" },
    { kind: "Connection", name: "weather", generation: 1, reconciled: 1, status: "Connected", detail: "open-meteo · External Service" },
    { kind: "Secret", name: "mqtt-credentials", generation: 2, reconciled: 2, status: "Ready", detail: "keys: username, password" },
    { kind: "Secret", name: "weather-api-key", generation: 1, reconciled: 1, status: "Ready", detail: "keys: apiKey" },
    { kind: "AssetType", name: "Train", generation: 5, reconciled: 5, status: "Ready", detail: "2 instances" },
    { kind: "AssetType", name: "PassengerCar", generation: 4, reconciled: 4, status: "Ready", detail: "4 instances" },
    { kind: "AssetType", name: "BrakeUnit", generation: 2, reconciled: 2, status: "Ready", detail: "4 instances" },
    { kind: "Device", name: "train-101-c1-brake", generation: 1, reconciled: 1, status: "Online", detail: "modbus-tcp/BrakeController · train-101-car-1-brakes" },
    { kind: "Device", name: "train-101-tcms", generation: 2, reconciled: 2, status: "Online", detail: "modbus-tcp/Tcms · train-101" },
    { kind: "Device", name: "wash-pump", generation: 1, reconciled: 1, status: "Offline", detail: "mqtt/Relay · wash-bay" },
    { kind: "Device", name: "spare-gps-7", generation: 1, reconciled: 1, status: "Online", detail: "mqtt/GpsTracker · not attached" },
    { kind: "Flow", name: "weather-to-depot", generation: 2, reconciled: 2, status: "Idle", detail: "schedule every 10 min · single" },
    { kind: "Flow", name: "service-schedule", generation: 9, reconciled: 8, status: "Running (1)", detail: "Event depot-mqtt/timetable · queue" },
    { kind: "Flow", name: "brake-test", generation: 3, reconciled: 3, status: "Idle", detail: "Data Point fleet/*/brakes.pressure · restart" },
  ],
  "plant-north": [
    { kind: "Connection", name: "plc-modbus", generation: 1, reconciled: 1, status: "Connected", detail: "modbus-tcp · 1 Device" },
    { kind: "AssetType", name: "HydraulicPress", generation: 1, reconciled: 1, status: "Ready", detail: "1 instance" },
    { kind: "Device", name: "press-1-plc", generation: 1, reconciled: 1, status: "Online", detail: "modbus-tcp/Plc · press-1" },
    { kind: "Flow", name: "shift-report", generation: 4, reconciled: 4, status: "Idle", detail: "schedule 06:00, 14:00, 22:00 · single" },
  ],
  default: [
    { kind: "Connection", name: "hue-bridge", generation: 1, reconciled: 1, status: "Connected", detail: "philips-hue · 3 Devices" },
    { kind: "Device", name: "hue-living", generation: 1, reconciled: 1, status: "Online", detail: "philips-hue/Bulb · living-room" },
  ],
};

export const discoveries: Record<string, string[]> = {
  "rail-depot": ["tcms-modbus: unit 17 at 10.0.4.17", "depot-mqtt: gps/tracker-8"],
  "plant-north": [],
  default: ["hue-bridge: Hue bulb 00:17:88:01:aa"],
};

export const users = [
  { name: "dan", grants: "rail-depot: Viewer", disabled: false },
  { name: "ada", grants: "rail-depot: Operator · plant-north: Operator", disabled: false },
  { name: "ben", grants: "rail-depot: Engineer · plant-north: Viewer", disabled: false },
  { name: "cara", grants: "rail-depot: Operator, Engineer", disabled: false },
  { name: "eve", grants: "Administrator", disabled: false },
  { name: "fay", grants: "Administrator · default: Engineer, Operator · rail-depot: Engineer", disabled: false },
  { name: "old-contractor", grants: "—", disabled: true },
];

export function findAsset(nodes: AssetNode[], name: string, path: AssetNode[] = []): AssetNode[] | null {
  for (const n of nodes) {
    if (n.name === name) return [...path, n];
    const hit = findAsset(n.children, name, [...path, n]);
    if (hit) return hit;
  }
  return null;
}

export const installation = {
  name: "depot-edge-01",
  id: "inst_7f3c2a91-4e0b-4c8e-9d61-2b5f0c8e1a44",
  version: "0.0.0-dev+02a1112",
  uptime: "12 days 4 h",
  silos: [
    { name: "silo-a", address: "10.0.4.2:11111", state: "Active", cpu: 23, memory: 41 },
    { name: "silo-b", address: "10.0.4.3:11111", state: "Active", cpu: 17, memory: 38 },
  ],
  integrations: [
    { name: "mqtt", version: "1.4.0", deviceTypes: 6 },
    { name: "modbus-tcp", version: "0.9.2", deviceTypes: 3 },
    { name: "philips-hue", version: "2.1.0", deviceTypes: 4 },
    { name: "open-meteo", version: "1.0.1", deviceTypes: 0 },
  ],
  controlCenter: null as string | null,
};

export interface Change {
  at: string;
  who: string;
  namespace: string | null;
  what: string;
  generation?: number;
  state: "Reconciling" | "Reconciled" | "Rejected" | "Done";
  note?: string;
}

export const recentChanges: Change[] = [
  { at: "14:00:41", who: "cara", namespace: "rail-depot", what: "Flow/service-schedule", generation: 9, state: "Reconciling", note: "1 Flow Execution still on Generation 8" },
  { at: "13:52:08", who: "ben", namespace: "rail-depot", what: "AssetType/Train, Asset/train-102", generation: 5, state: "Reconciled" },
  { at: "13:47:30", who: "ben", namespace: "rail-depot", what: "Connection/tcms-modbus", state: "Rejected", note: "409: Generation 7 changed since you read it" },
  { at: "11:05:12", who: "ada", namespace: "plant-north", what: "Acknowledged Alarm on press-1", state: "Done" },
  { at: "09:30:00", who: "eve", namespace: null, what: "Granted ben Viewer in plant-north", state: "Done" },
  { at: "09:12:55", who: "fay", namespace: "default", what: "Device/hue-living", generation: 1, state: "Reconciled" },
];
